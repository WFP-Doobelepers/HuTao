using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Zhongli.Data;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Logging;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors
{
    public class UserLoggingBehavior :
        INotificationHandler<GuildMemberUpdatedNotification>,
        INotificationHandler<UserJoinedNotification>,
        INotificationHandler<UserLeftNotification>
    {
        private readonly LoggingService _logging;

        public UserLoggingBehavior(LoggingService logging) { _logging = logging; }

        public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
        {
            await _logging.LogAsync(notification, cancellationToken);
        }

        public async Task Handle(UserJoinedNotification notification, CancellationToken cancellationToken)
        {
            await _logging.LogAsync(notification, cancellationToken);
        }

        public async Task Handle(UserLeftNotification notification, CancellationToken cancellationToken)
        {
            await _logging.LogAsync(notification, cancellationToken);
        }
    }
}