using System.Threading;
using System.Threading.Tasks;
using HuTao.Services.Core.Messages;
using HuTao.Services.Logging;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class MessageLoggingBehavior(LoggingService logging)
    : INotificationHandler<MessageDeletedNotification>,
      INotificationHandler<MessagesBulkDeletedNotification>,
      INotificationHandler<MessageReceivedNotification>,
      INotificationHandler<MessageUpdatedNotification>
{
    public Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
        => logging.LogAsync(notification, cancellationToken);

    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        => logging.LogAsync(notification, cancellationToken);

    public Task Handle(MessagesBulkDeletedNotification notification, CancellationToken cancellationToken)
        => logging.LogAsync(notification, cancellationToken);

    public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
        => logging.LogAsync(notification, cancellationToken);
}