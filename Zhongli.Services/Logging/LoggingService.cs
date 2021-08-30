using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Reaction;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Core.Messages;
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
            _db     = db;
        }

        public async Task PublishLogAsync(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Message is not IUserMessage message) return;
            if (message.Author is not IGuildUser user || user.IsBot) return;

            var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);
            await LogMessageAsync(userEntity, message, LogType.Created, cancellationToken);
        }

        public async Task PublishLogAsync(SocketReaction reaction, LogType logType, CancellationToken cancellationToken)
        {
            var reactionUser = reaction.User.GetValueOrDefault();
            if (reactionUser is not IGuildUser { IsBot: true } user || reaction.Channel is not IGuildChannel channel)
                return;

            var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);
            var log = await LogReactionAsync(userEntity, reaction, logType, cancellationToken);
            await PublishLogAsync(log, channel.Guild, cancellationToken);
        }

        public async Task PublishLogAsync(MessageDeletedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Channel is not IGuildChannel channel) return;

            var latest = await GetLatestMessage(notification.Message.Id, cancellationToken);
            if (latest is null) return;

            await UpdateLogAsync(latest, LogType.Deleted, cancellationToken);
            await PublishLogAsync(latest, channel.Guild, cancellationToken);
        }

        public async Task PublishLogAsync(MessageUpdatedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.NewMessage is not IUserMessage { Channel: IGuildChannel channel } message) return;
            if (message.Author is not IGuildUser user || user.IsBot) return;

            var latest = await GetLatestMessage(message.Id, cancellationToken);
            if (latest is null) return;

            if (latest.Embeds.Count != message.Embeds.Count)
            {
                await UpdateLogEmbedsAsync(latest, message, cancellationToken);
                return;
            }

            var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);
            var log = await LogMessageAsync(userEntity, message, latest, cancellationToken);
            await PublishLogAsync(log, channel.Guild, cancellationToken);
        }

        private EmbedLog AddDetails(EmbedBuilder embed, MessageLog log)
        {
            var user = _client.GetUser(log.UserId);

            embed
                .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
                .AddContent(log.Content)
                .AddField("Message", log.JumpUrlMarkdown())
                .AddField("Created", log.Timestamp.ToUniversalTimestamp(), true);

            if (log.EditedTimestamp is not null)
                embed.AddField("Edited", $"{log.EditedTimestamp.Value.ToUniversalTimestamp()}", true);

            var urls = log.Embeds.Select(e => e.Url);
            return new EmbedLog(embed.AddImages(log), string.Join(Environment.NewLine, urls));
        }

        private EmbedLog AddDetails(EmbedBuilder embed, ReactionLog log)
        {
            var user = _client.GetUser(log.UserId);
            var reaction = log.Emote;

            embed
                .WithUserAsAuthor(user, AuthorOptions.IncludeId)
                .AddContent($"{reaction} {Format.Code($"{reaction}")}")
                .AddField("Message", log.JumpUrlMarkdown(), true)
                .AddField("Channel", log.ChannelMentionMarkdown(), true)
                .AddField("Date", log.LogDate.ToUniversalTimestamp());

            if (reaction is EmoteEntity emote)
                embed.WithThumbnailUrl(CDN.GetEmojiUrl(emote.EmoteId, emote.IsAnimated));

            return new EmbedLog(embed);
        }

        private EmbedLog BuildLog(ILog log)
        {
            var embed = new EmbedBuilder()
                .WithTitle(log.GetTitle())
                .WithColor(log.LogType.GetColor())
                .WithCurrentTimestamp();

            return log switch
            {
                MessageLog message   => AddDetails(embed, message),
                ReactionLog reaction => AddDetails(embed, reaction),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(log), log, "Invalid log type.")
            };
        }

        private async Task PublishLogAsync(ILog? log, IGuild guild, CancellationToken cancellationToken)
        {
            if (log is null) return;

            var embed = BuildLog(log);
            await PublishLogAsync(embed, guild, cancellationToken);
        }

        private async Task PublishLogAsync(EmbedLog log, IGuild guild, CancellationToken cancellationToken)
        {
            var guildEntity = await _db.Guilds.TrackGuildAsync(guild, cancellationToken);
            var channelId = guildEntity.LoggingRules.MessageLogChannelId;
            if (channelId is null) return;

            var channel = await guild.GetTextChannelAsync(channelId.Value);
            if (channel is null) return;

            var (embed, content) = log;
            var message = await channel.SendMessageAsync(embed: embed.Build());
            if (!string.IsNullOrWhiteSpace(content)) await message.ReplyAsync(content);
        }

        private async Task UpdateLogAsync(ILog log, LogType type, CancellationToken cancellationToken)
        {
            log.LogDate = DateTimeOffset.Now;
            log.LogType = type;

            await _db.SaveChangesAsync(cancellationToken);
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

        private async Task<MessageLog?> LogMessageAsync(GuildUserEntity user, IUserMessage message, MessageLog oldLog,
            CancellationToken cancellationToken)
        {
            oldLog.UpdatedLog = await LogMessageAsync(user, message, LogType.Updated, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return oldLog.UpdatedLog;
        }

        private async Task<MessageLog> LogMessageAsync(GuildUserEntity user, IUserMessage message, LogType type,
            CancellationToken cancellationToken)
        {
            var log = _db.Add(new MessageLog(user, message, type)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            return log;
        }

        private async Task<ReactionLog?> LogReactionAsync(GuildUserEntity user, SocketReaction reaction,
            LogType logType,
            CancellationToken cancellationToken)
        {
            var emote = await _db.TrackEmoteAsync(reaction.Emote, cancellationToken);

            var log = _db.Add(new ReactionLog(user, reaction, logType, emote)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            return log;
        }

        private ValueTask<MessageLog?> GetLatestMessage(ulong messageId, CancellationToken cancellationToken)
            => GetLatestLogAsync<MessageLog>(m => m.MessageId == messageId, cancellationToken);

        private async ValueTask<T?> GetLatestLogAsync<T>(Func<T, bool> filter,
            CancellationToken cancellationToken) where T : class, ILog
        {
            return await _db.Set<T>().AsAsyncEnumerable()
                .OrderByDescending(l => l.LogDate)
                .FirstOrDefaultAsync(filter, cancellationToken);
        }

        private record EmbedLog(EmbedBuilder Embed, string? Content = null);
    }
}