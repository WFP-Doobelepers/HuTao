using System;
using Discord.WebSocket;
using MediatR;

namespace Zhongli.Services.Core.Messages;

/// <summary>
///     Describes an application-wide notification that occurs when <see cref="DiscordSocketClient.Disconnected" /> is
///     raised.
/// </summary>
public class DisconnectedNotification : INotification
{
    public DisconnectedNotification(Exception exception) { Exception = exception; }

    public Exception Exception { get; }
}