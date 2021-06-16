using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Serilog;

namespace Zhongli.Services.Core.Listeners
{
    public class ZhongliMediator : Mediator
    {
        public ZhongliMediator(ServiceFactory serviceFactory) : base(serviceFactory) { }

        protected override async Task PublishCore(
            IEnumerable<Func<INotification, CancellationToken, Task>> handlers,
            INotification notification, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var handler in handlers)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await handler(notification, cancellationToken);
                        }
                        catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
                        {
                            Log.Error(ex,
                                "An unexpected error occurred within a handler for a dispatched message: {notification}",
                                notification);
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
            {
                Log.Error(ex, "An unexpected error occurred while dispatching a notification: {notification}",
                    notification);
            }
        }
    }
}