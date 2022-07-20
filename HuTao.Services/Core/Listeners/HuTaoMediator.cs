using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
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
                var priorities = handlers.Select(h => (Handler: h, GetOrderAttribute(h)?.Priority));
                var ordered = priorities.OrderBy(h => !h.Priority.HasValue).ThenBy(h => h.Priority);
                foreach (var handler in ordered)
                {
                    try
                    {
                        await handler.Handler(notification, cancellationToken);
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

    private static PriorityAttribute? GetOrderAttribute(Func<INotification, CancellationToken, Task> x)
    {
        var handlerFieldInfo = x.Target?.GetType().GetField("x");
        if (handlerFieldInfo is not { FieldType.IsGenericType: true, FieldType.GenericTypeArguments.Length: 1 })
            return null;

        var type = handlerFieldInfo.GetValue(x.Target)?.GetType();
        var methods = type?.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        var method = methods?.FirstOrDefault(m =>
        {
            if (m is not { Name: "Handle" } || m.ReturnType != typeof(Task)) return false;

            var parameters = m.GetParameters();
            if (parameters.Length != 2) return false;

            var first = parameters[0];
            var second = parameters[1];

            return first.ParameterType == handlerFieldInfo.FieldType.GenericTypeArguments[0]
                && second.ParameterType == typeof(CancellationToken);
        });

        return method?.GetCustomAttribute<PriorityAttribute>();
    }
}