using Discord;
using MediatR;
using Zhongli.Data.Models.Moderation.Reprimands;

namespace Zhongli.Bot.Modules.Moderation
{
    public class WarnNotification : INotification
    {
        public WarnNotification(IGuildUser user, Warning warning)
        {
            User    = user;
            Warning = warning;
        }

        public IGuildUser User { get; }

        public Warning Warning { get; }
    }
}