using Discord.WebSocket;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zhongli.Services.Core.Messages
{
    /// <summary>
    ///     Describes an application-wide notification that occurs when <see cref="BaseSocketClient.UserUpdated" /> is raised.
    /// </summary>
    public class UserUpdatedNotification : INotification
    {
        public UserUpdatedNotification(SocketUser oldUser, SocketUser newUser)
        {
            OldUser = oldUser ?? throw new ArgumentNullException(nameof(oldUser));
            NewUser = newUser ?? throw new ArgumentNullException(nameof(newUser));
        }

        public SocketUser OldUser { get; }

        public SocketUser NewUser { get; }
    }
}
