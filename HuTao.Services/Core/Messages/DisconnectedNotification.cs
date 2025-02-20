using System;
using Discord.WebSocket;
using MediatR;

namespace HuTao.Services.Core.Messages;

/// <summary>
///     Describes an application-wide notification that occurs when <see cref="DiscordSocketClient.Disconnected" /> is
///     raised.
/// </summary>
public class DisconnectedNotification(Exception exception) : INotification
{
    public Exception Exception { get; } = exception;
}