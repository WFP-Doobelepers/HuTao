using Discord;
using MediatR;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Services.Core.Messages
{
    public class ReprimandRequest<T> : IRequest<ReprimandAction> where T : IReprimand
    {
        public ReprimandRequest(IGuildUser user, IGuildUser moderator, T reprimand)
        {
            User      = user;
            Reprimand = reprimand;
            Moderator = moderator;
        }

        public IGuildUser Moderator { get; }

        public IGuildUser User { get; }

        public T Reprimand { get; }
    }
}