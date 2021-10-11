using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging
{
    public class UserUpdatedLog : UserLog
    {
        protected UserUpdatedLog() { }

        public UserUpdatedLog(SocketUser oldUser, SocketUser newUser)
        {
            LogDate = DateTimeOffset.Now;

            OldUser = new GuildUserEntity(oldUser);
            OldAvatarURL = oldUser.GetAvatarUrl();

            User = new GuildUserEntity(newUser);
            AvatarURL = newUser.GetAvatarUrl();
        }
        

        public UserUpdatedLog(SocketGuildUser oldUser, SocketGuildUser newUser) : base(newUser)
        {
            OldUser = new GuildUserEntity((IGuildUser) oldUser);
            OldAvatarURL = oldUser.GetAvatarUrl();
<<<<<<< HEAD
        }

        public string OldAvatarURL { get; set; }
=======

            OldRoles = oldUser.Roles.ToHashSet();
        }

        public string OldAvatarURL { get; }

        public HashSet<SocketRole>? OldRoles { get; }
>>>>>>> 71c63f8d0c30fe479a229a8382a6bbc087aff1d8

    }
}
