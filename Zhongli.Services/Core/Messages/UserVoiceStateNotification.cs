using Discord.WebSocket;
using MediatR;

namespace Zhongli.Services.Core.Messages
{
    public class UserVoiceStateNotification : INotification
    {
        public UserVoiceStateNotification(SocketUser user, SocketVoiceState old, SocketVoiceState @new)
        {
            User = user;
            Old  = old;
            New  = @new;
        }

        public SocketUser User { get; }

        public SocketVoiceState New { get; }

        public SocketVoiceState Old { get; }
    }
}