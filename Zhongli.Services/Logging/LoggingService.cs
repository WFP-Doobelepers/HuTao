using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
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
using Embed = Zhongli.Data.Models.Discord.Message.Embed;

namespace Zhongli.Services.Logging
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _client;
        private readonly ZhongliContext _db;

        public LoggingService(DiscordSocketClient client, ZhongliContext db)
        {
            _client = client;
            _db = db;
        }

        public async Task LogAsync(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
        {
            var oldUser = notification.OldMember;
            var newUser = notification.NewMember;

            if (oldUser.Nickname == newUser.Nickname) return;

            var log = LogUser(oldUser, newUser, cancellationToken);
            await PublishLogAsync(log, LogType.UserUpdated, newUser.Guild, cancellationToken);
        }

        public async Task LogAsync(UserUpdatedNotification notification, CancellationToken cancellationToken)
        {
            var oldUser = notification.OldUser;
            var newUser = notification.NewUser;

            if (oldUser.ToString() == newUser.ToString() && oldUser.GetAvatarUrl() == newUser.GetAvatarUrl()) return;

            var log = LogUser(oldUser, newUser, cancellationToken);
            await PublishLogAsync(log, LogType.UserUpdated, oldUser.MutualGuilds, cancellationToken);
        }

        public async Task LogAsync(UserLeftNotification notification, CancellationToken cancellationToken)
        {
            var user = notification.GuildUser;
            var log = LogLeft(user, cancellationToken);
            await PublishLogAsync(log, LogType.UserLeft, user.Guild, cancellationToken);
        }

        public async Task LogAsync(UserJoinedNotification notification, CancellationToken cancellationToken)
        {
            var user = notification.GuildUser;
            var log = LogUser(user, cancellationToken);
            await PublishLogAsync(log, LogType.UserJoined, user.Guild, cancellationToken);
        }

        public async Task LogAsync(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Message is not IUserMessage { Channel: INestedChannel channel } message) return;
            if (message.Author is not IGuildUser user || user.IsBot) return;
            if (await IsExcludedAsync(channel, user, cancellationToken)) return;

            var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);
            await LogMessageAsync(userEntity, message, cancellationToken);
        }

        public async Task LogAsync(ReactionAddedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Channel is not INestedChannel channel) return;

            var reaction = notification.Reaction;

            var user = reaction.User.GetValueOrDefault();
            if (user is not IGuildUser guildUser || guildUser.IsBot) return;
            if (await IsExcludedAsync(channel, guildUser, cancellationToken)) return;

            var message = await notification.Message.GetOrDownloadAsync();
            var metadata = message.Reactions[reaction.Emote];
            if (metadata.ReactionCount > 1) return;

            var userEntity = await _db.Users.TrackUserAsync(guildUser, cancellationToken);
            var log = await LogReactionAsync(userEntity, reaction, cancellationToken);
            await PublishLogAsync(log, LogType.ReactionAdded, channel.Guild, cancellationToken);
        }

        public async Task LogAsync(ReactionRemovedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Channel is not IGuildChannel channel) return;

            var log = await LogDeletionAsync(notification.Reaction, null, cancellationToken);
            await PublishLogAsync(log, LogType.ReactionRemoved, channel.Guild, cancellationToken);
        }

        public async Task LogAsync(MessageDeletedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Channel is not IGuildChannel channel) return;

            var message = notification.Message.Value;
            var details = await TryGetAuditLogDetails(message, channel.Guild);

            var latest = await GetLatestMessage(notification.Message.Id, cancellationToken);
            if (latest is null) return;

            var log = await LogDeletionAsync(latest, details, cancellationToken);
            await PublishLogAsync(log, LogType.MessageDeleted, channel.Guild, cancellationToken);
        }

        public async Task LogAsync(MessageUpdatedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.NewMessage is not IUserMessage { Channel: INestedChannel channel } message) return;
            if (message.Author is not IGuildUser user || user.IsBot) return;
            if (await IsExcludedAsync(channel, user, cancellationToken)) return;

            var latest = await GetLatestMessage(message.Id, cancellationToken);
            if (latest is null) return;

            if (latest.Embeds.Count != message.Embeds.Count)
            {
                await UpdateLogEmbedsAsync(latest, message, cancellationToken);
                return;
            }

            var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);
            var log = await LogMessageAsync(userEntity, message, latest, cancellationToken);
            await PublishLogAsync(log, LogType.MessageUpdated, channel.Guild, cancellationToken);
        }

        public async Task LogAsync(MessagesBulkDeletedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Channel is not IGuildChannel channel) return;

            var details = await TryGetAuditLogDetails(notification.Messages.Count, channel);
            var logs = await notification.Messages.ToAsyncEnumerable()
                .SelectAwait(async m => await GetLatestMessage(m.Id, cancellationToken))
                .ToListAsync(cancellationToken);

            var log = await LogDeletionAsync(logs.OfType<MessageLog>(), channel, details, cancellationToken);
            await PublishLogAsync(log, LogType.MessagesBulkDeleted, channel.Guild, cancellationToken);
        }

        private EmbedLog AddDetails(EmbedBuilder embed, UserLog log)
        {
            var user = _client.GetUser(log.User.Id);

            StringBuilder content = new StringBuilder()
                .AppendLine($"Nickname: {log.User.Nickname}")
                .AppendLine($"Username: {log.User}")
                .AppendLine($"Avatar: {log.AvatarURL}");


            embed.WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
                .WithTitle(log.GetTitle())
                .AddField("User", content.ToString())
                .WithColor(log.DidLeave ? Color.Red : Color.Green);

            var updated = log.OldUser;
            if(updated is not null)
            {
                UserUpdatedLog oldUserLog = (UserUpdatedLog) log;

                content = new StringBuilder();

                //Append Changes
                if (log.User.Nickname != log.OldUser.Nickname) content.AppendLine($"Nickname: {log.OldUser.Nickname}");
                if (log.User.Username != log.OldUser.Username) content.AppendLine($"Username: {log.OldUser.Username}");
                if (oldUserLog.OldAvatarURL != log.AvatarURL) content.AppendLine($"Avatar: {oldUserLog.OldAvatarURL}");

                embed.AddField("Before", content.ToString())
                    .WithColor(Color.Purple);
            }

            return new EmbedLog(embed);
        }

        private EmbedLog AddDetails(EmbedBuilder embed, MessageLog log)
        {
            var user = _client.GetUser(log.UserId);

            embed
                .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
                .AddContent(log.Content);

            var updated = log.UpdatedLog;
            if (updated is not null)
            {
                var date = updated.EditedTimestamp ?? updated.LogDate;
                embed
                    .AddField("After", updated.Content)
                    .AddField("Edited", date.ToUniversalTimestamp(), true);
            }

            embed
                .AddField("Created", log.Timestamp.ToUniversalTimestamp(), true)
                .AddField("Message", log.JumpUrlMarkdown(), true);

            var urls = log.Embeds.Select(e => e.Url);
            return new EmbedLog(embed.AddImages(log), string.Join(Environment.NewLine, urls));
        }

        private EmbedLog AddDetails<T>(EmbedBuilder embed, T log) where T : ILog, IReactionEntity
        {
            var user = _client.GetUser(log.UserId);
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

        private EmbedLog BuildLog(ILog log)
        {
            var embed = new EmbedBuilder()
                .WithTitle(log.GetTitle())
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();

            return log switch
            {
                MessageLog message => AddDetails(embed, message),
                ReactionLog reaction => AddDetails(embed, reaction),
                UserLog user => AddDetails(embed, user),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(log), log, "Invalid log type.")
            };
        }

        private static MemoryStream GenerateStreamFromString(string value)
            => new(Encoding.Unicode.GetBytes(value));

        private async Task PublishLogAsync(DeleteLog? log, LogType type, IGuild guild,
            CancellationToken cancellationToken)
        {
            if (log is null) return;

            var embed = await BuildLogAsync(log, cancellationToken);
            var channel = await GetLoggingChannelAsync(type, guild, cancellationToken);

            await PublishLogAsync(embed, channel);
        }

        private async Task PublishLogAsync(ILog? log, LogType type, IEnumerable<IGuild> guilds, CancellationToken cancellationToken)
        {
            if (log is null) return;

            var embed = BuildLog(log);
            foreach (IGuild guild in guilds) await PublishLogAsync(embed, await GetLoggingChannelAsync(type, guild, cancellationToken));

        }

        private async Task PublishLogAsync(ILog? log, LogType type, IGuild guild, CancellationToken cancellationToken)
        {
            if (log is null) return;

            var embed = BuildLog(log);
            var channel = await GetLoggingChannelAsync(type, guild, cancellationToken);

            await PublishLogAsync(embed, channel);
        }

        private static async Task PublishLogAsync(EmbedLog? log, IMessageChannel? channel)
        {
            if (log is null || channel is null) return;
            var (embed, content, attachment) = log;

            if (attachment is null)
                await channel.SendMessageAsync(content, embed: embed.Build());
            else
            {
                await channel.SendFileAsync(GenerateStreamFromString(attachment), "Messages.md",
                    content, embed: embed.Build());
            }
        }

        private async Task UpdateLogEmbedsAsync(MessageLog oldLog, IMessage message,
            CancellationToken cancellationToken)
        {
            foreach (var embed in message.Embeds)
            {
                oldLog.Embeds.Add(new Embed(embed));
            }

            await _db.SaveChangesAsync(cancellationToken);
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

        private async Task<DeleteLog?> LogDeletionAsync(IMessageEntity message, ActionDetails? details,
            CancellationToken cancellationToken)
        {
            var deleted = new MessageDeleteLog(message, details);

            _db.Add(deleted);
            await _db.SaveChangesAsync(cancellationToken);

            return deleted;
        }

        private async Task<DeleteLog?> LogDeletionAsync(IEnumerable<MessageLog> messages,
            IGuildChannel channel, ActionDetails? details,
            CancellationToken cancellationToken)
        {
            var deleted = new MessagesDeleteLog(messages, channel, details);

            _db.Add(deleted);
            await _db.SaveChangesAsync(cancellationToken);

            return deleted;
        }

        private async Task<DeleteLog?> LogDeletionAsync(SocketReaction reaction, ActionDetails? details,
            CancellationToken cancellationToken)
        {
            var emote = await _db.TrackEmoteAsync(reaction.Emote, cancellationToken);
            var deleted = new ReactionDeleteLog(emote, reaction, details);

            _db.Add(deleted);
            await _db.SaveChangesAsync(cancellationToken);

            return deleted;
        }

        private async Task<EmbedLog?> AddDetailsAsync(EmbedBuilder embed, IMessageEntity log,
            CancellationToken cancellationToken)
        {
            var deleted = await GetLatestMessage(log.MessageId, cancellationToken);
            return deleted is null ? null : AddDetails(embed, deleted);
        }

        private async Task<EmbedLog?> BuildLogAsync(DeleteLog log, CancellationToken cancellationToken)
        {
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

            return log switch
            {
                MessagesDeleteLog messages => await AddDetailsAsync(embed, messages, cancellationToken),
                MessageDeleteLog message => await AddDetailsAsync(embed, message, cancellationToken),
                ReactionDeleteLog reaction => AddDetails(embed, reaction),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(log), log, "Invalid log type.")
            };
        }

        private async Task<EmbedLog> AddDetailsAsync(EmbedBuilder embed, MessagesDeleteLog log,
            CancellationToken cancellationToken)
        {
            var logs = await log.Messages
                .ToAsyncEnumerable()
                .SelectAwait(async m => await GetLatestMessage(m.MessageId, cancellationToken))
                .ToListAsync(cancellationToken);

            embed
                .AddField("Created", log.LogDate.ToUniversalTimestamp(), true)
                .AddField("Message Count", log.Messages.Count, true);

            var messages = logs.OfType<MessageLog>().GetDetails();
            return new EmbedLog(embed, Attachment: messages.ToString());
        }

        private static async Task<IAuditLogEntry?> TryGetAuditLogEntry(IMessage? message, IGuild guild)
        {
            if (message is null) return null;

            var audits = await guild
                .GetAuditLogsAsync(actionType: ActionType.MessageDeleted);

            return audits
                .Where(e => DateTimeOffset.Now - e.CreatedAt < TimeSpan.FromMinutes(1))
                .FirstOrDefault(e
                    => e.Data is MessageDeleteAuditLogData d
                    && d.Target.Id == message.Author.Id
                    && d.ChannelId == message.Channel.Id);
        }

        private static async Task<IAuditLogEntry?> TryGetAuditLogEntry(
            int messageCount, IGuildChannel channel)
        {
            var audits = await channel.Guild
                .GetAuditLogsAsync(actionType: ActionType.MessageBulkDeleted);

            return audits
                .Where(e => DateTimeOffset.Now - e.CreatedAt < TimeSpan.FromMinutes(1))
                .FirstOrDefault(e
                    => e.Data is MessageBulkDeleteAuditLogData d
                    && d.MessageCount >= messageCount
                    && d.ChannelId == channel.Id);
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

        private async Task<ReactionLog?> LogReactionAsync(GuildUserEntity user, SocketReaction reaction,
            CancellationToken cancellationToken)
        {
            var emote = await _db.TrackEmoteAsync(reaction.Emote, cancellationToken);

            var log = _db.Add(new ReactionLog(user, reaction, emote)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            return log;
        }

        private UserLog LogUser(SocketUser oldUser, SocketUser newUser, CancellationToken cancellationToken)
        {
            return new UserUpdatedLog(oldUser, newUser);
        }

        private UserLog LogUser(SocketGuildUser oldUser, SocketGuildUser newUser, CancellationToken cancellationToken)
        {
            return new UserUpdatedLog(oldUser, newUser);
        }

        private UserLog LogUser(SocketGuildUser socketGuildUser, CancellationToken cancellationToken)
        {
            return new UserJoinedLog(socketGuildUser);
        }

        private UserLog LogLeft(SocketGuildUser socketGuildUser, CancellationToken cancellationToken)
        {
            return new UserLeftLog(socketGuildUser);
        }

        private ValueTask<MessageLog?> GetLatestMessage(ulong messageId, CancellationToken cancellationToken)
            => GetLatestLogAsync<MessageLog>(m => m.MessageId == messageId, cancellationToken);

        private async ValueTask<T?> GetLatestLogAsync<T>(Expression<Func<T, bool>> filter,
            CancellationToken cancellationToken) where T : class, ILog
        {
            return await _db.Set<T>().AsQueryable()
                .OrderByDescending(l => l.LogDate)
                .FirstOrDefaultAsync(filter, cancellationToken);
        }

        private record EmbedLog(EmbedBuilder Embed, string? Content = null, string? Attachment = null);
    }
}