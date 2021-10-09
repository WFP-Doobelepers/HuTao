using Discord.WebSocket;
using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging
{
    public class UserLeftLog : UserLog
    {
        protected UserLeftLog() { }

        public UserLeftLog(SocketGuildUser socketGuildUser) : base(socketGuildUser)
        {
            DidLeave = true;
        }
    }
}
