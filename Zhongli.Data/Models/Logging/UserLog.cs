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
    public class UserLog : ILog
    {
        protected UserLog() { }

        public UserLog(SocketGuildUser socketGuildUser)
        {
            LogDate = DateTimeOffset.Now;
            User = new GuildUserEntity((IGuildUser) socketGuildUser);
            AvatarURL = socketGuildUser.GetAvatarUrl();
            Roles = socketGuildUser.Roles.ToHashSet();
        }

        public GuildUserEntity User { get; set; }

        public GuildUserEntity? OldUser { get; set; } = null;

        public string AvatarURL { get; set; }

        public bool DidLeave { get; set; }

        public DateTimeOffset LogDate { get; set; }

        public HashSet<SocketRole> Roles { get; }
    }
}
