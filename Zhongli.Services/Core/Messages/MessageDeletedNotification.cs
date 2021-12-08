using System;
using Discord;
using Discord.WebSocket;
using MediatR;

namespace Zhongli.Services.Core.Messages;

/// <summary>
///     Describes an application-wide notification that occurs when <see cref="BaseSocketClient.MessageDeleted" /> is
///     raised.
/// </summary>
public class MessageDeletedNotification : INotification
{
    /// <summary>
    ///     Constructs a new <see cref="MessageDeletedNotification" /> from the given values.
    /// </summary>
    /// <param name="message">The value to use for <see cref="Message" />.</param>
    /// <param name="channel">The value to use for <see cref="Channel" />.</param>
    /// <exception cref="ArgumentNullException">Throws for <paramref name="message" /> and <paramref name="channel" />.</exception>
    public MessageDeletedNotification(Cacheable<IMessage, ulong>? message, ISocketMessageChannel channel)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    /// <summary>
    ///     A cache entry for the message that was deleted.
    /// </summary>
    public Cacheable<IMessage, ulong> Message { get; }

    /// <summary>
    ///     The channel from which the message was deleted.
    /// </summary>
    public ISocketMessageChannel Channel { get; }
}