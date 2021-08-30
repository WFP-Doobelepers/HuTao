using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
            => _logging.PublishLogAsync(notification, cancellationToken);

        public Task Handle(ReactionRemovedNotification notification, CancellationToken cancellationToken)
            => _logging.PublishLogAsync(notification, cancellationToken);
    }
}