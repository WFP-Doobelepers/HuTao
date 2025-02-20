using System;
using Discord;
using MediatR;

namespace HuTao.Services.AutoRemoveMessage;

public class RemovableMessageSentNotification(IMessage message, IUser[] users) : INotification
{
    public IMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

    public IUser[] Users { get; } = users ?? throw new ArgumentNullException(nameof(users));
}