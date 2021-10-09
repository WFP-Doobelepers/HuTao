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

        public UserUpdatedLog(SocketGuildUser oldUser, SocketGuildUser newUser) : base(newUser)
        {
            OldUser = new GuildUserEntity(oldUser);
            OldAvatarURL = oldUser.GetAvatarUrl();
        }

        public string OldAvatarURL { get; set; }

    }
}
