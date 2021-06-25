using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Bot.Behaviors
{
    public class AuthorizationConfigureBehavior :
        INotificationHandler<GuildAvailableNotification>,
        INotificationHandler<JoinedGuildNotification>
    {
        private readonly AuthorizationService _auth;

        public AuthorizationConfigureBehavior(AuthorizationService auth) { _auth = auth; }

        public async Task Handle(GuildAvailableNotification notification, CancellationToken cancellationToken)
        {
            await _auth.AutoConfigureGuild(notification.Guild.Id, cancellationToken);
        }

        public async Task Handle(JoinedGuildNotification notification, CancellationToken cancellationToken)
        {
            await _auth.AutoConfigureGuild(notification.Guild.Id, cancellationToken);
        }
    }
}