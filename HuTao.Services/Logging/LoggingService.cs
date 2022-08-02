using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message;
using HuTao.Data.Models.Discord.Reaction;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Core;
using HuTao.Services.Core.Messages;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static HuTao.Services.Utilities.EmbedBuilderOptions;
using Attachment = HuTao.Data.Models.Discord.Message.Attachment;

namespace HuTao.Services.Logging;

public class LoggingService
{
    private const EmbedBuilderOptions LogEmbedOptions = UseProxy | EnlargeThumbnails | ReplaceAnimations;
    private readonly DiscordSocketClient _client;
    private readonly HttpClient _http;
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;

    public LoggingService(DiscordSocketClient client, HttpClient http, HuTaoContext db, IMemoryCache cache)
    {
        _client = client;
        _http   = http;
        _db     = db;
        _cache  = cache;
    }

    public async Task LogAsync(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message is not IUserMessage
            {
                Author: IGuildUser user,
                Author.IsBot: false,
                Channel: INestedChannel channel
            } message) return;

        if (await IsExcludedAsync(channel, user, cancellationToken)) return;
        await LogMessageAsync(user, message, cancellationToken);
    }

    public async Task LogAsync(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        var message = await GetMessageAsync(notification.Message);
        if (message is not
            {
                Author: IGuildUser user,
                Author.IsBot: false,
                Channel: INestedChannel channel
            }) return;

        var reaction = notification.Reaction;
        if (message.Reactions.TryGetValue(reaction.Emote, out var metadata) && metadata.ReactionCount > 1)
            return;

        var reactor = reaction.User.GetValueOrDefault();
        if (reactor is not IGuildUser { IsBot: false } guildUser) return;

        if (await IsExcludedAsync(channel, user, cancellationToken)) return;
        var userEntity = await _db.Users.TrackUserAsync(guildUser, cancellationToken);
        var log = await LogReactionAsync(userEntity, reaction, cancellationToken);
        await PublishLogAsync(log, LogType.ReactionAdded, channel.Guild, cancellationToken);
    }

    public async Task LogAsync(ReactionRemovedNotification notification, CancellationToken cancellationToken)
    {
        var message = await GetMessageAsync(notification.Message);
        if (message is not
            {
                Author: IGuildUser user,
                Author.IsBot: false,
                Channel: INestedChannel channel
            }) return;

        var reaction = notification.Reaction;
        if (message.Reactions.ContainsKey(reaction.Emote))
            return;

        if (await IsExcludedAsync(channel, user, cancellationToken)) return;
        var log = await LogDeletionAsync(reaction, null, cancellationToken);
        await PublishLogAsync(new ReactionDeleteDetails(log, channel.Guild), cancellationToken);
    }

    public async Task LogAsync(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        if (await notification.Channel.GetOrDownloadAsync() is not INestedChannel channel) return;
        if (await IsExcludedAsync(channel, null, cancellationToken)) return;

        var message = notification.Message.Value;
        var details = await TryGetAuditLogDetails(message, channel.Guild);

        var latest = await GetLatestMessage(channel.Guild.Id, channel.Id, notification.Message.Id, cancellationToken);
        if (latest is null) return;

        var log = await LogDeletionAsync(latest, details, cancellationToken);
        await PublishLogAsync(new MessageDeleteDetails(latest, log, channel.Guild), cancellationToken);
    }

    public async Task LogAsync(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.NewMessage is not IUserMessage
            {
                Author: IGuildUser user,
                Author.IsBot: false,
                Channel: INestedChannel channel
            } message) return;

        if (await IsExcludedAsync(channel, user, cancellationToken)) return;
        var latest = await GetLatestMessage(channel.Guild.Id, channel.Id, message.Id, cancellationToken);

        if (latest is null) return;
        if (await MessageUnchanged(latest, message, cancellationToken)) return;

        var log = await LogMessageAsync(user, message, latest, cancellationToken);
        await PublishLogAsync(log, LogType.MessageUpdated, channel.Guild, cancellationToken);
    }

    public async Task LogAsync(MessagesBulkDeletedNotification notification, CancellationToken cancellationToken)
    {
        if (await notification.Channel.GetOrDownloadAsync() is not INestedChannel channel) return;
        if (await IsExcludedAsync(channel, null, cancellationToken)) return;

        var details = await TryGetAuditLogDetails(notification.Messages.Count, channel);
        var messages = GetLatestMessages(channel, notification.Messages.Select(x => x.Id)).ToList();

        var log = await LogDeletionAsync(messages, channel, details, cancellationToken);
        await PublishLogAsync(new MessagesDeleteDetails(messages, log, channel.Guild), cancellationToken);
    }

    public async Task<IUser?> GetUserAsync<T>(T? log) where T : IMessageEntity
        => log is null ? null : await ((IDiscordClient) _client).GetUserAsync(log.UserId);

    public ValueTask<MessageLog?> GetLatestMessage(
        ulong guildId, ulong channelId, ulong messageId,
        CancellationToken cancellationToken = default)
        => GetLatestLogAsync<MessageLog>(m
                => m.GuildId == guildId
                && m.ChannelId == channelId
                && m.MessageId == messageId,
            cancellationToken);

    private IEnumerable<MessageLog> GetLatestMessages(IGuildChannel channel, IEnumerable<ulong> messageIds)
        => _db.Set<MessageLog>()
            .Where(m => m.GuildId == channel.Guild.Id)
            .Where(m => m.ChannelId == channel.Id)
            .Where(m => m.UpdatedLog == null)
            .Where(m => messageIds.Contains(m.MessageId))
            .OrderByDescending(m => m.LogDate);

    private async Task PublishLogAsync(DeleteDetails details, CancellationToken cancellationToken)
    {
        var guildEntity = await _db.Guilds.TrackGuildAsync(details.Guild, cancellationToken);
        var options = guildEntity.LoggingRules?.UploadAttachments is true
            ? LogEmbedOptions | UploadAttachments
            : LogEmbedOptions;

        var embed = await BuildLogAsync(details, options);
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

        var embed = await BuildLogAsync(log, LogEmbedOptions);
        var channel = await GetLoggingChannelAsync(type, guild, cancellationToken);

        await PublishLogAsync(embed, channel);
    }

    private static async Task PublishLogAsync(EmbedLog? log, IMessageChannel? channel)
    {
        if (log is null || channel is null) return;
        await channel.SendFilesAsync(log.Attachments, embeds: log.Embeds.ToArray());
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

    private async Task<bool> IsExcludedAsync(
        INestedChannel channel, IGuildUser? user,
        CancellationToken cancellationToken)
    {
        var guild = channel.Guild;

        var exclusions = await GetExclusionsAsync(guild, cancellationToken);
        return exclusions.Any(e => e.Judge(channel, user));
    }

    private async Task<bool> MessageUnchanged(MessageLog log, IMessage message, CancellationToken cancellationToken)
    {
        var embeds = message.Embeds
            .Select(embed => new Data.Models.Discord.Message.Embeds.Embed(embed))
            .ToList();

        var attachments = message.Attachments
            .Select(attachment => new Attachment(attachment))
            .ToList();

        var embedsEqual = embeds.SequenceEqual(log.Embeds);
        var attachmentsEqual = attachments.SequenceEqual(log.Attachments);

        log.Embeds      = embedsEqual ? log.Embeds : embeds;
        log.Attachments = attachmentsEqual ? log.Attachments : attachments;
        await _db.SaveChangesAsync(cancellationToken);

        return message.Content == log.Content && embedsEqual && attachmentsEqual;
    }

    private async Task<EmbedLog> AddDetailsAsync(EmbedBuilder embed, ILog log, IReadOnlyCollection<MessageLog> logs)
    {
        embed
            .AddField("Created", log.LogDate.ToUniversalTimestamp(), true)
            .AddField("Message Count", logs.Count, true);

        var content = new StringBuilder()
            .AppendLine("# Message Bulk Deleted")
            .AppendLine($"- Created: {log.LogDate} {log.LogDate.Humanize()}")
            .AppendLine($"- Message Count: {logs.Count}")
            .Append(await logs.GetDetailsAsync(_client));

        return new EmbedLog(embed, content: content.ToString());
    }

    private async Task<EmbedLog> AddDetailsAsync(EmbedBuilder embed, MessageLog log, EmbedBuilderOptions options)
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

        if (log.ReferencedMessageId is not null)
        {
            var reply = await GetLatestMessage(log.GuildId, log.ChannelId, log.ReferencedMessageId.Value);
            var replyUser = await GetUserAsync(reply);

            embed.WithMessageReference(log, reply, replyUser);
        }

        var attachments = log.Attachments.Chunk(4).SelectMany(attachments => attachments.ToEmbedBuilders(options));
        var embeds = log.Embeds.Where(e => e.IsViewable()).Select(e => e.ToBuilder(options));
        var files = options.HasFlag(UploadAttachments)
            ? await log.Attachments.ToAsyncEnumerable()
                .Select(a => (Url: options.HasFlag(UseProxy) ? a.ProxyUrl : a.Url, Name: a.Filename))
                .SelectAwait(async a => new FileAttachment(await _http.GetStreamAsync(a.Url), a.Name))
                .ToListAsync()
            : Enumerable.Empty<FileAttachment>();

        return new EmbedLog(embed, attachments.Concat(embeds), attachments: files);
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

    private async Task<EmbedLog> BuildLogAsync(DeleteDetails details, EmbedBuilderOptions options)
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
            MessagesDeleteDetails messages => await AddDetailsAsync(embed, messages.Log, messages.Messages),
            MessageDeleteDetails message => await AddDetailsAsync(embed, message.Message, options),
            ReactionDeleteDetails reaction => await AddDetailsAsync(embed, reaction.Log),
            _ => throw new ArgumentOutOfRangeException(nameof(log), log, "Invalid log type.")
        };
    }

    private async Task<EmbedLog> BuildLogAsync(ILog log, EmbedBuilderOptions options)
    {
        var embed = new EmbedBuilder()
            .WithTitle(log.GetTitle())
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();

        return log switch
        {
            MessageLog message   => await AddDetailsAsync(embed, message, options),
            ReactionLog reaction => await AddDetailsAsync(embed, reaction),
            _ => throw new ArgumentOutOfRangeException(
                nameof(log), log, "Invalid log type.")
        };
    }

    private static async Task<IAuditLogEntry?> TryGetAuditLogEntry(IMessage? message, IGuild guild)
    {
        if (message is null) return null;

        try
        {
            var audits = await guild.GetAuditLogsAsync(actionType: ActionType.MessageDeleted);
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
        catch (NullReferenceException)
        {
            return null;
        }
    }

    private static async Task<IAuditLogEntry?> TryGetAuditLogEntry(
        int messageCount, IGuildChannel channel)
    {
        try
        {
            var audits = await channel.Guild.GetAuditLogsAsync(actionType: ActionType.MessageBulkDeleted);
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
        catch (NullReferenceException)
        {
            return null;
        }
    }

    private async Task<IEnumerable<Criterion>> GetExclusionsAsync(IGuild guild, CancellationToken cancellationToken)
    {
        var rules = await _db.Guilds.GetLoggingAsync(guild, _cache, cancellationToken);
        return rules?.LoggingExclusions ?? Enumerable.Empty<Criterion>();
    }

    private async Task<IMessageChannel?> GetLoggingChannelAsync(LogType type, IGuild guild,
        CancellationToken cancellationToken)
    {
        var guildEntity = await _db.Guilds.TrackGuildAsync(guild, cancellationToken);

        var channel = guildEntity.LoggingRules?.LoggingChannels.FirstOrDefault(r => r.Type == type);
        if (channel is null) return null;

        return await guild.GetTextChannelAsync(channel.ChannelId);
    }

    private async Task<MessageDeleteLog> LogDeletionAsync(IMessageEntity message, ActionDetails? details,
        CancellationToken cancellationToken)
    {
        var deleted = _db.Add(new MessageDeleteLog(message, details)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        return deleted;
    }

    private async Task<MessageLog?> LogMessageAsync(IGuildUser user,
        IUserMessage message, MessageLog oldLog,
        CancellationToken cancellationToken)
    {
        oldLog.UpdatedLog = await LogMessageAsync(user, message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return oldLog;
    }

    private async Task<MessageLog> LogMessageAsync(IGuildUser user, IUserMessage message,
        CancellationToken cancellationToken)
    {
        var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);

        var log = _db.Add(new MessageLog(userEntity, message)).Entity;
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
        => await _cache.GetOrCreateAsync(cached.Id, async cacheEntry =>
        {
            cacheEntry.SlidingExpiration = TimeSpan.FromMinutes(1);
            return await cached.GetOrDownloadAsync();
        });

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

    private record EmbedLog(IEnumerable<Embed> Embeds, IEnumerable<FileAttachment> Attachments)
    {
        public EmbedLog(
            EmbedBuilder embed, IEnumerable<EmbedBuilder>? embeds = null, string? content = null,
            IEnumerable<FileAttachment>? attachments = null) : this(
            (embeds?.Select(e => e.Build()) ?? Enumerable.Empty<Embed>()).Prepend(embed.Build()),
            attachments ?? Enumerable.Empty<FileAttachment>())
        {
            if (!string.IsNullOrEmpty(content))
                Attachments = Attachments.Prepend(Attachment(content)).Take(10);
        }

        private static FileAttachment Attachment(string content) => new(Stream(content), "messages.md");

        private static MemoryStream Stream(string value) => new(Encoding.UTF8.GetBytes(value));
    }
}