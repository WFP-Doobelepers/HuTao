using Discord;
using MediatR;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Services.Core.Messages
{
    public class WarnNotification : INotification
    {
        public WarnNotification(IGuildUser user, IGuildUser moderator, Warning warning)
        {
            User      = user;
            Warning   = warning;
            Moderator = moderator;
        }

        public IGuildUser Moderator { get; }

        public IGuildUser User { get; }

        public Warning Warning { get; }
    }
}