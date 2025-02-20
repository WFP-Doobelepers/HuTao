using System.Threading;
using System.Threading.Tasks;
using HuTao.Services.Core.Messages;
using HuTao.Services.Logging;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class ReactionLoggingBehavior(LoggingService logging)
    : INotificationHandler<ReactionAddedNotification>,
      INotificationHandler<ReactionRemovedNotification>
{
    public Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
        => logging.LogAsync(notification, cancellationToken);

    public Task Handle(ReactionRemovedNotification notification, CancellationToken cancellationToken)
        => logging.LogAsync(notification, cancellationToken);
}