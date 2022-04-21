using System.Threading;
using System.Threading.Tasks;
using HuTao.Services.Core.Messages;
using HuTao.Services.Logging;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class MessageLoggingBehavior :
    INotificationHandler<MessageDeletedNotification>,
    INotificationHandler<MessagesBulkDeletedNotification>,
    INotificationHandler<MessageReceivedNotification>,
    INotificationHandler<MessageUpdatedNotification>
{
    private readonly LoggingService _logging;

    public MessageLoggingBehavior(LoggingService logging) { _logging = logging; }

    public Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
        => _logging.LogAsync(notification, cancellationToken);

    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        => _logging.LogAsync(notification, cancellationToken);

    public Task Handle(MessagesBulkDeletedNotification notification, CancellationToken cancellationToken)
        => _logging.LogAsync(notification, cancellationToken);

    public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
        => _logging.LogAsync(notification, cancellationToken);
}