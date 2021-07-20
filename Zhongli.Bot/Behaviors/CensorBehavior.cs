using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;
using Zhongli.Services.Quote;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors
{
    public class CensorBehavior :
        INotificationHandler<MessageReceivedNotification>,
        INotificationHandler<MessageUpdatedNotification>
    {
        private readonly ZhongliContext _db;
        private readonly ModerationService _moderationService;

        public CensorBehavior(ZhongliContext db, ModerationService moderationService)
        {
            _db                = db;
            _moderationService = moderationService;
        }

        public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
            => ProcessMessage(notification.Message, cancellationToken);

        public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
            => ProcessMessage(notification.NewMessage, cancellationToken);

        private async Task ProcessMessage(SocketMessage message, CancellationToken cancellationToken = default)
        {
            var author = message.Author;
            if (author.IsBot || author.IsWebhook || author is not IGuildUser user)
                return;

            var guild = ((IGuildChannel) message.Channel).Guild;
            var guildEntity = await _db.Guilds.FindByIdAsync(guild.Id, cancellationToken);
            if (guildEntity is null || cancellationToken.IsCancellationRequested)
                return;

            await _db.Users.TrackUserAsync(user, cancellationToken);

            var currentUser = await guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(user, currentUser, ModerationSource.Censor, "[Censor trigger]");

            foreach (var censor in guildEntity.AutoModerationRules.Censors
                .Where(c => c.Exclusions.All(e => !e.Judge((ITextChannel) message.Channel, user)))
                .Where(c => c.IsMatch(message)))
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await message.DeleteAsync();
                await LogCensor(message, censor, guildEntity, guild, cancellationToken);

                switch (censor)
                {
                    case BanCensor ban:
                        await _moderationService.TryBanAsync(ban.DeleteDays, details, cancellationToken);
                        return;
                    case KickCensor:
                        await _moderationService.TryKickAsync(details, cancellationToken);
                        return;
                    case MuteCensor mute:
                        await _moderationService.TryMuteAsync(mute.Length, details, cancellationToken);
                        break;
                    case WarnCensor warn:
                        await _moderationService.WarnAsync(warn.Amount, details, cancellationToken);
                        break;
                }
            }
        }

        private static async Task LogCensor(IMessage message, Censor censor, GuildEntity guildEntity, IGuild guild,
            CancellationToken cancellationToken = default)
        {
            var channelId = guildEntity.LoggingRules.ModerationChannelId;
            if (channelId is null || cancellationToken.IsCancellationRequested)
                return;

            var logChannel = await guild.GetTextChannelAsync(channelId.Value);
            if (logChannel is null || cancellationToken.IsCancellationRequested)
                return;

            var content = Regex.Replace(
                message.Content, censor.Pattern,
                m => Format.Bold(m.Value),
                censor.Options);

            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Censor Triggered").WithDescription(content)
                .AddMeta(message, AuthorOptions.IncludeId).AddJumpLink(message)
                .WithFooter(censor.Id.ToString(), guild.IconUrl);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }
    }
}