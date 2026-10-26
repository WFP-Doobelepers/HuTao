using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using HuTao.Services.Core.Messages;
using MediatR;

namespace HuTao.Services.Logging;

public class MessageLogCleanupBehavior : INotificationHandler<ReadyNotification>
{
    public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        RecurringJob.AddOrUpdate<MessageLogCleanupService>(
            MessageLogCleanupService.JobId, service => service.RunAsync(), Cron.Hourly);

        return Task.CompletedTask;
    }
}
