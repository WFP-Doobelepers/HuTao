using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Logging;

namespace Zhongli.Bot.Behaviors
{
    public class ReactionLoggingBehavior :
        INotificationHandler<ReactionAddedNotification>,
        INotificationHandler<ReactionRemovedNotification>
    {
        private readonly LoggingService _logging;

        public ReactionLoggingBehavior(LoggingService logging) { _logging = logging; }

        public Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
            => _logging.PublishLogAsync(notification.Reaction, LogType.Created, cancellationToken);

        public Task Handle(ReactionRemovedNotification notification, CancellationToken cancellationToken)
            => _logging.PublishLogAsync(notification.Reaction, LogType.Deleted, cancellationToken);
    }
}