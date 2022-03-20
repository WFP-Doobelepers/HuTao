using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Logging;

namespace Zhongli.Bot.Behaviors;

public class JoinLoggingBehavior :
    INotificationHandler<UserJoinedNotification>
{
    private readonly LoggingService _loggingService;

    public JoinLoggingBehavior(LoggingService loggingService) { _loggingService = loggingService; }

    public Task Handle(UserJoinedNotification notification, CancellationToken cancellationToken)
        => _loggingService.LogAsync(notification, cancellationToken);
}