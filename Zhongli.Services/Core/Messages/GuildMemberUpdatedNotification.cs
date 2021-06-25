using Discord.WebSocket;
using MediatR;

namespace Zhongli.Services.Core.Messages
{
    /// <summary>
    ///     Describes an application-wide notification that occurs when <see cref="BaseSocketClient.GuildMemberUpdated" /> is
    ///     raised.
    /// </summary>
    public class GuildMemberUpdatedNotification : INotification
    {
        /// <summary>
        ///     Constructs a new <see cref="GuildMemberUpdatedNotification" /> from the given values.
        /// </summary>
        /// <param name="oldMember">The value to use for <see cref="OldMember" />.</param>
        /// <param name="newMember">The value to use for <see cref="NewMember" />.</param>
        public GuildMemberUpdatedNotification(
            SocketGuildUser oldMember,
            SocketGuildUser newMember)
        {
            OldMember = oldMember;
            NewMember = newMember;
        }

        /// <summary>
        ///     A model of the Guild Member that was updated, from after the update.
        /// </summary>
        public SocketGuildUser NewMember { get; }

        /// <summary>
        ///     A model of the Guild Member that was updated, from before the update.
        /// </summary>
        public SocketGuildUser OldMember { get; }
    }
}