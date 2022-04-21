using System;
using Discord.WebSocket;
using MediatR;

namespace HuTao.Services.Core.Messages;

/// <summary>
///     Describes an application-wide notification that occurs when <see cref="BaseSocketClient.UserLeft" /> is raised.
/// </summary>
public class UserLeftNotification : INotification
{
    /// <summary>
    ///     Constructs a new <see cref="UserLeftNotification" /> from the given values.
    /// </summary>
    /// <param name="guild">The value to use for <see cref="SocketGuild" />.</param>
    /// <param name="user">The value to use for <see cref="SocketUser" />.</param>
    /// <exception cref="ArgumentNullException">Throws for <paramref name="guild" />.</exception>
    /// <exception cref="ArgumentNullException">Throws for <paramref name="user" />.</exception>
    public UserLeftNotification(SocketGuild guild, SocketUser user)
    {
        Guild = guild ?? throw new ArgumentNullException(nameof(guild));
        User  = user ?? throw new ArgumentNullException(nameof(user));
    }

    /// <summary>
    ///     The guild that was left.
    /// </summary>
    public SocketGuild Guild { get; }

    /// <summary>
    ///     The user that left.
    /// </summary>
    public SocketUser User { get; }
}