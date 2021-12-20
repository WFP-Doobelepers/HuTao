using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using MediatR;

namespace Zhongli.Services.Core.Messages;

/// <summary>
///     Describes an application-wide notification that occurs when <see cref="BaseSocketClient.MessagesBulkDeleted" /> is
///     raised.
/// </summary>
public class MessagesBulkDeletedNotification : INotification
{
    /// <summary>
    ///     Constructs a new <see cref="MessagesBulkDeletedNotification" /> from the given values.
    /// </summary>
    /// <param name="messages">The value to use for <see cref="Messages" />.</param>
    /// <param name="channel">The value to use for <see cref="Channel" />.</param>
    /// <exception cref="ArgumentNullException">Throws for <paramref name="messages" /> and <paramref name="channel" />.</exception>
    public MessagesBulkDeletedNotification(IReadOnlyCollection<Cacheable<IMessage, ulong>> messages,
        Cacheable<IMessageChannel, ulong> channel)
    {
        Messages = messages;
        Channel  = channel;
    }

    /// <summary>
    ///     The channel from which the message was deleted.
    /// </summary>
    public Cacheable<IMessageChannel, ulong> Channel { get; }

    /// <summary>
    ///     A cache entry for the messages that were deleted.
    /// </summary>
    public IReadOnlyCollection<Cacheable<IMessage, ulong>> Messages { get; }
}