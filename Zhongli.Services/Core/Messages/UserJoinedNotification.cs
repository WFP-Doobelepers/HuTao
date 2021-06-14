using System;
using Discord.WebSocket;
using MediatR;

namespace Zhongli.Services.Core.Messages
{
    /// <summary>
    ///     Describes an application-wide notification that occurs when <see cref="BaseSocketClient.UserJoined" /> is raised.
    /// </summary>
    public class UserJoinedNotification : INotification
    {
        /// <summary>
        ///     Constructs a new <see cref="UserJoinedNotification" /> from the given values.
        /// </summary>
        /// <param name="guildUser">The value to use for <see cref="GuildUser" />.</param>
        /// <exception cref="ArgumentNullException">Throws for <paramref name="guildUser" />.</exception>
        public UserJoinedNotification(SocketGuildUser guildUser)
        {
            GuildUser = guildUser ?? throw new ArgumentNullException(nameof(guildUser));
        }

        /// <summary>
        ///     The user that joined, and the guild that was joined.
        /// </summary>
        public SocketGuildUser GuildUser { get; }
    }
}