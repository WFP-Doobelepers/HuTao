using System;
using Discord;
using MediatR;

namespace HuTao.Services.AutoRemoveMessage;

public class RemovableMessageRemovedNotification : INotification
{
    public RemovableMessageRemovedNotification(IMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public IMessage Message { get; }
}