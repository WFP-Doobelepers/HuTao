using System;
using Discord.WebSocket;
using MediatR;

namespace Zhongli.Services.Core.Messages;

/// <summary>
///     Describes an application-wide notification that occurs when <see cref="BaseSocketClient.UserUnbanned" /> is raised.
/// </summary>
public class UserUnbannedNotification : INotification
{
    /// <summary>
    ///     Constructs a new <see cref="UserBannedNotification" /> from the given values.
    /// </summary>
    /// <param name="user">The value to use for <see cref="User" />.</param>
    /// <param name="guild">The value to use for <see cref="Guild" />.</param>
    /// <exception cref="ArgumentNullException">Throws for <paramref name="user" /> and <paramref name="guild" />.</exception>
    public UserUnbannedNotification(SocketUser user, SocketGuild guild)
    {
        User  = user ?? throw new ArgumentNullException(nameof(user));
        Guild = guild ?? throw new ArgumentNullException(nameof(guild));
    }

    /// <summary>
    ///     The guild from which the user was unbanned.
    /// </summary>
    public SocketGuild Guild { get; }

    /// <summary>
    ///     The user that was unbanned.
    /// </summary>
    public SocketUser User { get; }
}