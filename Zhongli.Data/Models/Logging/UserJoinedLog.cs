using Discord.WebSocket;
using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging
{
    public class UserJoinedLog : UserLog
    {
        protected UserJoinedLog() { }

        public UserJoinedLog(SocketGuildUser socketGuildUser) : base(socketGuildUser)
        {
        }
    }
}
