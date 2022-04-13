using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Data.Models.Discord.Reaction;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;
using Zhongli.Services.Quote;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Logging;

public class LoggingService
{
    private readonly DiscordSocketClient _client;
    private readonly IMemoryCache _memoryCache;
    private readonly ZhongliContext _db;
    public LoggingService(DiscordSocketClient client, IMemoryCache memoryCache, ZhongliContext db)
    {
        _client      = client;
        _memoryCache = memoryCache;
        _db          = db;
    }

    public async Task LogAsync(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message is not IUserMessage { Channel: INestedChannel channel } message) return;
        if (message.Author is not IGuildUser { Username: { } } user || user.IsBot) return;
        if (await IsExcludedAsync(channel, user, cancellationToken)) return;

        var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);
        await LogMessageAsync(userEntity, message, cancellationToken);
    }

    public async Task LogAsync(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        var message = await GetMessageAsync(notification.Message);
        if (message.Channel is not INestedChannel channel) return;

        var reaction = notification.Reaction;
        if (message.Reactions.TryGetValue(reaction.Emote, out var metadata) && metadata.ReactionCount > 1)
            return;

        var user = reaction.User.GetValueOrDefault();
        if (user is not IGuildUser guildUser || guildUser.IsBot) return;
        if (await IsExcludedAsync(channel, guildUser, cancellationToken)) return;

        var userEntity = await _db.Users.TrackUserAsync(guildUser, cancellationToken);
        var log = await LogReactionAsync(userEntity, reaction, cancellationToken);
        await PublishLogAsync(log, LogType.ReactionAdded, channel.Guild, cancellationToken);
    }

    public async Task LogAsync(ReactionRemovedNotification notification, CancellationToken cancellationToken)
    {
        var message = await GetMessageAsync(notification.Message);
        if (message.Channel is not IGuildChannel channel) return;

        var reaction = notification.Reaction;
        if (message.Reactions.ContainsKey(reaction.Emote))
            return;

        var log = await LogDeletionAsync(reaction, null, cancellationToken);
        await PublishLogAsync(new ReactionDeleteDetails(log, channel.Guild), cancellationToken);
    }

    public async Task LogAsync(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        if (await notification.Channel.GetOrDownloadAsync() is not IGuildChannel channel) return;

        var message = notification.Message.Value;
        var details = await TryGetAuditLogDetails(message, channel.Guild);

        var latest = await GetLatestMessage(notification.Message.Id, cancellationToken);
        if (latest is null) return;

        var log = await LogDeletionAsync(latest, details, cancellationToken);
        await PublishLogAsync(new MessageDeleteDetails(latest, log, channel.Guild), cancellationToken);
    }

    public async Task LogAsync(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.NewMessage is not IUserMessage { Channel: INestedChannel channel } message) return;
        if (message.Author is not IGuildUser { Username: { } } user || user.IsBot) return;
        if (await IsExcludedAsync(channel, user, cancellationToken)) return;

        var latest = await GetLatestMessage(message.Id, cancellationToken);
        if (latest is null) return;

        if (await LogEmbedsUpdated(latest, message, cancellationToken))
            return;

        var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);
        var log = await LogMessageAsync(userEntity, message, latest, cancellationToken);
        await PublishLogAsync(log, LogType.MessageUpdated, channel.Guild, cancellationToken);
    }

    public async Task LogAsync(MessagesBulkDeletedNotification notification, CancellationToken cancellationToken)
    {
        if (await notification.Channel.GetOrDownloadAsync() is not IGuildChannel channel) return;

        var details = await TryGetAuditLogDetails(notification.Messages.Count, channel);
        var messages = GetLatestMessages(channel, notification.Messages.Select(x => x.Id)).ToList();

        var log = await LogDeletionAsync(messages, channel, details, cancellationToken);
        await PublishLogAsync(new MessagesDeleteDetails(messages, log, channel.Guild), cancellationToken);
    }

    public async Task LogAsync(UserJoinedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.GuildUser is not IGuildUser user) return;

        var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);

        await PublishLogAsync(new UserJoinLog(userEntity, user.JoinedAt),
            LogType.GuildUserJoined, user.Guild, cancellationToken);
    }

    private static EmbedLog AddDetails(EmbedBuilder embed, ILog log, IReadOnlyCollection<MessageLog> logs)
    {
        embed
            .AddField("Created", log.LogDate.ToUniversalTimestamp(), true)
            .AddField("Message Count", logs.Count, true);

        var content = logs.GetDetails();
        return new EmbedLog(embed, content.ToString());
    }

    private IEnumerable<MessageLog> GetLatestMessages(IGuildChannel channel, IEnumerable<ulong> messageIds)
        => _db.Set<MessageLog>()
            .Where(m => m.GuildId == channel.Guild.Id)
            .Where(m => m.ChannelId == channel.Id)
            .Where(m => m.UpdatedLog == null)
            .Where(m => messageIds.Contains(m.MessageId))
            .OrderByDescending(m => m.LogDate);

    private static MemoryStream GenerateStreamFromString(string value)
        => new(Encoding.UTF8.GetBytes(value));

    private async Task PublishLogAsync(DeleteDetails details,
        CancellationToken cancellationToken)
    {
        var embed = await BuildLogAsync(details);
        var channel = await GetLoggingChannelAsync(details switch
        {
            MessageDeleteDetails  => LogType.MessageDeleted,
            MessagesDeleteDetails => LogType.MessagesBulkDeleted,
            ReactionDeleteDetails => LogType.ReactionRemoved,
            _                     => throw new ArgumentOutOfRangeException(nameof(details))
        }, details.Guild, cancellationToken);

        await PublishLogAsync(embed, channel);
    }

    private async Task PublishLogAsync(ILog? log, LogType type, IGuild guild, CancellationToken cancellationToken)
    {
        if (log is null) return;

        var embed = await BuildLogAsync(log);
        var channel = await GetLoggingChannelAsync(type, guild, cancellationToken);

        await PublishLogAsync(embed, channel);
    }

    private static async Task PublishLogAsync(EmbedLog? log, IMessageChannel? channel)
    {
        if (log is null || channel is null) return;
        var (embeds, attachment) = log;

        if (attachment is null)
            await channel.SendMessageAsync(embeds: embeds);
        else
            await channel.SendFileAsync(GenerateStreamFromString(attachment), "Messages.md", embeds: embeds);
    }

    private static async Task<ActionDetails?> TryGetAuditLogDetails(IMessage? message, IGuild guild)
    {
        var entry = await TryGetAuditLogEntry(message, guild);
        if (entry is null) return null;

        var user = await guild.GetUserAsync(entry.User.Id);
        return new ActionDetails(user, entry.Reason);
    }

    private static async Task<ActionDetails?> TryGetAuditLogDetails(
        int messageCount, IGuildChannel channel)
    {
        var entry = await TryGetAuditLogEntry(messageCount, channel);
        if (entry is null) return null;

        var user = await channel.Guild.GetUserAsync(entry.User.Id);
        return new ActionDetails(user, entry.Reason);
    }

    private async Task<bool> IsExcludedAsync(INestedChannel channel, IGuildUser user,
        CancellationToken cancellationToken)
    {
        var guild = channel.Guild;

        var guildEntity = await _db.Guilds.FindByIdAsync(guild.Id, cancellationToken);
        if (guildEntity is null || cancellationToken.IsCancellationRequested) return false;

        return guildEntity.LoggingRules.LoggingExclusions.Any(e => e.Judge(channel, user));
    }

    private async Task<bool> LogEmbedsUpdated(MessageLog log, IMessage message, CancellationToken cancellationToken)
    {
        var embeds = message.Embeds
            .Select(embed => new Data.Models.Discord.Message.Embeds.Embed(embed))
            .ToList();

        var embedsChanged = !embeds.SequenceEqual(log.Embeds);

        log.Embeds = embeds;
        await _db.SaveChangesAsync(cancellationToken);

        return message.Content == log.Content && embedsChanged;
    }

    private async Task<EmbedLog> AddDetailsAsync(EmbedBuilder embed, MessageLog log)
    {
        var user = await GetUserAsync(log);

        embed
            .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
            .AddContent(log.Content);

        var updated = log.UpdatedLog;
        if (updated is not null)
        {
            var date = updated.EditedTimestamp ?? updated.LogDate;
            embed
                .AddField("After", updated.Content.Truncate(EmbedFieldBuilder.MaxFieldValueLength))
                .AddField("Edited", date.ToUniversalTimestamp(), true);
        }

        embed
            .AddField("Created", log.Timestamp.ToUniversalTimestamp(), true)
            .AddField("Message", log.JumpUrlMarkdown(), true);

        var attachments = log.Attachments.Chunk(4)
            .SelectMany(attachments => attachments.ToBuilder());

        var embeds = log.Embeds
            .Where(e => e.IsViewable())
            .Select(e => e.ToBuilder(EmbedBuilderOptions.UseProxy));

        return new EmbedLog(embed, attachments.Concat(embeds));
    }

    private async Task<EmbedLog> AddDetailsAsync<T>(EmbedBuilder embed, T log) where T : ILog, IReactionEntity
    {
        var user = await GetUserAsync(log);
        var reaction = log.Emote;

        embed
            .WithUserAsAuthor(user, AuthorOptions.IncludeId)
            .AddContent($"{reaction} {Format.Code($"{reaction}")}")
            .AddField("Message", log.JumpUrlMarkdown(), true)
            .AddField("Channel", log.ChannelMentionMarkdown(), true)
            .AddField("Date", log.LogDate.ToUniversalTimestamp(), true);

        if (reaction is EmoteEntity emote)
            embed.WithThumbnailUrl(CDN.GetEmojiUrl(emote.EmoteId, emote.IsAnimated));

        return new EmbedLog(embed);
    }

    private async Task<EmbedLog> AddDetailsAsync(EmbedBuilder embed, UserJoinLog log)
    {
        var user = await _client.GetUserAsync(log.UserId);

        embed
            .AddContent(new StringBuilder()
                .AppendLine($"{Format.Bold("`Mention:`")} <@{user.Id}>")
                .AppendLine($"{Format.Bold("`GuildId:`")} {log.GuildId}")
                .ToString())
            .AddField("AccountCreationDate", user.CreatedAt.ToUniversalTimestamp(), true)
            .AddField("JoinDate", log.JoinDate?.ToUniversalTimestamp(), true)
            .AddField("FirstJoinDate", log.FirstJoinDate?.ToUniversalTimestamp(), true)
            .WithUserAsAuthor(user, AuthorOptions.IncludeId);

        return new EmbedLog(embed);
    }

    private async Task<EmbedLog> BuildLogAsync(DeleteDetails details)
    {
        var log = details.Deleted;
        var embed = new EmbedBuilder()
            .WithTitle($"{log.GetTitle()} Deleted")
            .WithColor(Color.Red)
            .WithCurrentTimestamp();

        if (log.Action is not null)
        {
            embed
                .AddField("Deleted by", log.GetModerator(), true)
                .AddField("Deleted on", log.GetDate(), true)
                .AddField("Reason", log.GetReason(), true);
        }

        return details switch
        {
            MessagesDeleteDetails messages => AddDetails(embed, messages.Log, messages.Messages),
            MessageDeleteDetails message => await AddDetailsAsync(embed, message.Message),
            ReactionDeleteDetails reaction => await AddDetailsAsync(embed, reaction.Log),
            _ => throw new ArgumentOutOfRangeException(nameof(log), log, "Invalid log type.")
        };
    }

    private async Task<EmbedLog> BuildLogAsync(ILog log)
    {
        var embed = new EmbedBuilder()
            .WithTitle(log.GetTitle())
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();

        return log switch
        {
            MessageLog message   => await AddDetailsAsync(embed, message),
            ReactionLog reaction => await AddDetailsAsync(embed, reaction),
            UserJoinLog join     => await AddDetailsAsync(embed, join),
            _ => throw new ArgumentOutOfRangeException(
                nameof(log), log, "Invalid log type.")
        };
    }

    private static async Task<IAuditLogEntry?> TryGetAuditLogEntry(IMessage? message, IGuild guild)
    {
        if (message is null) return null;

        try
        {
            var audits = await guild
                .GetAuditLogsAsync(actionType: ActionType.MessageDeleted);

            return audits
                .Where(e => DateTimeOffset.UtcNow - e.CreatedAt < TimeSpan.FromMinutes(1))
                .FirstOrDefault(e
                    => e.Data is MessageDeleteAuditLogData d
                    && d.Target.Id == message.Author.Id
                    && d.ChannelId == message.Channel.Id);
        }
        catch (HttpException e) when (e.DiscordCode == DiscordErrorCode.InsufficientPermissions)
        {
            return null;
        }
    }

    private static async Task<IAuditLogEntry?> TryGetAuditLogEntry(
        int messageCount, IGuildChannel channel)
    {
        try
        {
            var audits = await channel.Guild
                .GetAuditLogsAsync(actionType: ActionType.MessageBulkDeleted);

            return audits
                .Where(e => DateTimeOffset.UtcNow - e.CreatedAt < TimeSpan.FromMinutes(1))
                .FirstOrDefault(e
                    => e.Data is MessageBulkDeleteAuditLogData d
                    && d.MessageCount >= messageCount
                    && d.ChannelId == channel.Id);
        }
        catch (HttpException e) when (e.DiscordCode == DiscordErrorCode.InsufficientPermissions)
        {
            return null;
        }
    }

    private async Task<IMessageChannel?> GetLoggingChannelAsync(LogType type, IGuild guild,
        CancellationToken cancellationToken)
    {
        var guildEntity = await _db.Guilds.TrackGuildAsync(guild, cancellationToken);

        var channel = guildEntity.LoggingRules.LoggingChannels
            .FirstOrDefault(r => r.Type == type);

        if (channel is null) return null;
        return await guild.GetTextChannelAsync(channel.ChannelId);
    }

    private async Task<IUser> GetUserAsync<T>(T log) where T : IMessageEntity
        => (IUser) _client.GetUser(log.UserId) ?? await _client.Rest.GetUserAsync(log.UserId);

    private async Task<MessageDeleteLog> LogDeletionAsync(IMessageEntity message, ActionDetails? details,
        CancellationToken cancellationToken)
    {
        var deleted = _db.Add(new MessageDeleteLog(message, details)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        return deleted;
    }

    private async Task<MessageLog?> LogMessageAsync(GuildUserEntity user,
        IUserMessage message, MessageLog oldLog,
        CancellationToken cancellationToken)
    {
        oldLog.UpdatedLog = await LogMessageAsync(user, message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return oldLog;
    }

    private async Task<MessageLog> LogMessageAsync(GuildUserEntity user, IUserMessage message,
        CancellationToken cancellationToken)
    {
        var log = _db.Add(new MessageLog(user, message)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        return log;
    }

    private async Task<MessagesDeleteLog> LogDeletionAsync(IEnumerable<MessageLog> messages,
        IGuildChannel channel, ActionDetails? details,
        CancellationToken cancellationToken)
    {
        var deleted = _db.Add(new MessagesDeleteLog(messages, channel, details)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        return deleted;
    }

    private async Task<ReactionDeleteLog> LogDeletionAsync(SocketReaction reaction, ActionDetails? details,
        CancellationToken cancellationToken)
    {
        var emote = await _db.TrackEmoteAsync(reaction.Emote, cancellationToken);
        var deleted = new ReactionDeleteLog(emote, reaction, details);

        _db.Add(deleted);
        await _db.SaveChangesAsync(cancellationToken);

        return deleted;
    }

    private async Task<ReactionLog?> LogReactionAsync(GuildUserEntity user, SocketReaction reaction,
        CancellationToken cancellationToken)
    {
        var emote = await _db.TrackEmoteAsync(reaction.Emote, cancellationToken);

        var log = _db.Add(new ReactionLog(user, reaction, emote)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        return log;
    }

    private async ValueTask<IUserMessage> GetMessageAsync(Cacheable<IUserMessage, ulong> cached)
        => await _memoryCache.GetOrCreateAsync(cached.Id, async cacheEntry =>
        {
            cacheEntry.SlidingExpiration = TimeSpan.FromMinutes(1);
            return await cached.GetOrDownloadAsync();
        });

    private ValueTask<MessageLog?> GetLatestMessage(ulong messageId, CancellationToken cancellationToken)
        => GetLatestLogAsync<MessageLog>(m => m.MessageId == messageId, cancellationToken);

    private async ValueTask<T?> GetLatestLogAsync<T>(Expression<Func<T, bool>> filter,
        CancellationToken cancellationToken) where T : class, ILog
        => await _db.Set<T>().AsQueryable()
            .OrderByDescending(l => l.LogDate)
            .FirstOrDefaultAsync(filter, cancellationToken);

    private record DeleteDetails(DeleteLog Deleted, IGuild Guild);

    private record ReactionDeleteDetails(ReactionDeleteLog Log, IGuild Guild)
        : DeleteDetails(Log, Guild);

    private record MessageDeleteDetails(MessageLog Message, DeleteLog Deleted, IGuild Guild)
        : DeleteDetails(Deleted, Guild);

    private record MessagesDeleteDetails(IReadOnlyCollection<MessageLog> Messages, MessagesDeleteLog Log, IGuild Guild)
        : DeleteDetails(Log, Guild);

    private record EmbedLog(Embed[] Embeds, string? Attachment = null)
    {
        public EmbedLog(EmbedBuilder embed, string? attachment = null)
            : this(new[] { embed.Build() }, attachment) { }

        public EmbedLog(EmbedBuilder embed, IEnumerable<EmbedBuilder> embeds)
            : this(new[] { embed.Build() }.Concat(embeds.Select(e => e.Build())).ToArray()) { }
    }
}