using System;
using Discord.WebSocket;
using MediatR;

namespace Zhongli.Services.Core.Messages;

/// <summary>
///     Describes an application-wide notification that occurs when <see cref="BaseSocketClient.InteractionCreated" /> is
///     raised.
/// </summary>
public class InteractionCreatedNotification : INotification
{
    /// <summary>
    ///     Constructs a new <see cref="InteractionCreatedNotification" /> from the given values.
    /// </summary>
    /// <param name="interaction">The value to use for <see cref="SocketInteraction" />.</param>
    /// <exception cref="ArgumentNullException">Throws for <paramref name="interaction" />.</exception>
    public InteractionCreatedNotification(SocketInteraction interaction)
    {
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

    /// <summary>
    ///     The <see cref="SocketInteraction" /> that was made.
    /// </summary>
    public SocketInteraction Interaction { get; }
}