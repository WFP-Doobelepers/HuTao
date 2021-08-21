using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors
{
    public class CensorBehavior :
        INotificationHandler<MessageReceivedNotification>,
        INotificationHandler<MessageUpdatedNotification>
    {
        private readonly ModerationService _moderation;
        private readonly ZhongliContext _db;

        public CensorBehavior(ModerationService moderation, ZhongliContext db)
        {
            _moderation = moderation;
            _db         = db;
        }

        public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
            => ProcessMessage(notification.Message, cancellationToken);

        public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
            => ProcessMessage(notification.NewMessage, cancellationToken);

        private async Task ProcessMessage(SocketMessage message, CancellationToken cancellationToken = default)
        {
            var author = message.Author;
            if (author.IsBot || author.IsWebhook
                || author is not IGuildUser user
                || message.Channel is not ITextChannel channel)
                return;

            var guild = channel.Guild;
            var guildEntity = await _db.Guilds.FindByIdAsync(guild.Id, cancellationToken);
            if (guildEntity is null || cancellationToken.IsCancellationRequested)
                return;

            if (guildEntity.ModerationRules.CensorExclusions.Any(e => e.Judge(channel, user)))
                return;

            await _db.Users.TrackUserAsync(user, cancellationToken);
            var currentUser = await guild.GetCurrentUserAsync();

            foreach (var censor in guildEntity.ModerationRules.Triggers.OfType<Censor>()
                .Where(c => c.Exclusions.All(e => !e.Judge(channel, user)))
                .Where(c => c.Regex().IsMatch(message.Content)))
            {
                var details = new ReprimandDetails(user, currentUser, "[Censor Triggered]", censor);
                var length = guildEntity.ModerationRules.CensorTimeRange;

                await _moderation.CensorAsync(message, length, details, cancellationToken);
            }
        }
    }
}