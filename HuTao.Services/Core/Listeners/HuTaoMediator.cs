using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Serilog;

namespace HuTao.Services.Core.Listeners;

public class HuTaoMediator : Mediator
{
    public HuTaoMediator(ServiceFactory serviceFactory) : base(serviceFactory) { }

    protected override Task PublishCore(
        IEnumerable<Func<INotification, CancellationToken, Task>> handlers,
        INotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _ = Task.Run(async () =>
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(notification, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
                    {
                        Log.Error(ex,
                            "An unexpected error occurred within a handler for a dispatched message: {Notification}",
                            notification);
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
        {
            Log.Error(ex, "An unexpected error occurred while dispatching a notification: {Notification}",
                notification);
        }

        return Task.CompletedTask;
    }
}