using Discord.Commands;
using MediatR;

namespace Zhongli.Services.Utilities
{
    public class NotificationContext<T> : INotification
    {
        public NotificationContext(T message, ICommandContext context)
        {
            Message = message;
            Context = context;
        }

        public ICommandContext Context { get; }

        public T Message { get; }
    }
}