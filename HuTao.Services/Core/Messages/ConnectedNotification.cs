using Discord.WebSocket;
using MediatR;

namespace HuTao.Services.Core.Messages;

/// <summary>
///     Describes an application-wide notification that occurs when <see cref="DiscordSocketClient.Connected" /> is
///     raised.
/// </summary>
public class ConnectedNotification : INotification
{
    /// <summary>
    ///     A default, reusable instance of the <see cref="ConnectedNotification" /> class.
    /// </summary>
    public static readonly ConnectedNotification Default
        = new();

    private ConnectedNotification() { }
}