using System.Threading;
using System.Threading.Tasks;
using Discord.Rest;
using MediatR;
using Zhongli.Data;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Logging;

namespace Zhongli.Bot.Behaviors
{
    public class MessageLoggingBehavior :
        INotificationHandler<MessageDeletedNotification>,
        INotificationHandler<MessagesBulkDeletedNotification>,
        INotificationHandler<MessageReceivedNotification>,
        INotificationHandler<MessageUpdatedNotification>
    {
        private readonly LoggingService _logging;
        private readonly ZhongliContext _db;

        public MessageLoggingBehavior(LoggingService logging, ZhongliContext db)
        {
            _logging = logging;
            _db      = db;
        }

        public Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
            => _logging.LogAsync(notification, cancellationToken);

        public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
            => _logging.LogAsync(notification, cancellationToken);

        public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
            => _logging.LogAsync(notification, cancellationToken);

        public Task Handle(MessagesBulkDeletedNotification notification, CancellationToken cancellationToken)
            => _logging.LogAsync(notification, cancellationToken);
    }
}