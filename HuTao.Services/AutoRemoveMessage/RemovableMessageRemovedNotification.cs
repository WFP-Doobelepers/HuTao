using System;
using Discord;
using MediatR;

namespace HuTao.Services.AutoRemoveMessage;

public class RemovableMessageRemovedNotification(IMessage message) : INotification
{
    public IMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));
}