using System.Threading;
using System.Threading.Tasks;
using HuTao.Services.Core.Messages;
using HuTao.Services.Logging;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class ReactionLoggingBehavior :
    INotificationHandler<ReactionAddedNotification>,
    INotificationHandler<ReactionRemovedNotification>
{
    private readonly LoggingService _logging;

    public ReactionLoggingBehavior(LoggingService logging) { _logging = logging; }

    public Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
        => _logging.LogAsync(notification, cancellationToken);

    public Task Handle(ReactionRemovedNotification notification, CancellationToken cancellationToken)
        => _logging.LogAsync(notification, cancellationToken);
}