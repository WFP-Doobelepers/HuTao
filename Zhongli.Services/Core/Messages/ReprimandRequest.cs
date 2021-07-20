using Discord;
using MediatR;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Services.Core.Messages
{
    public class ReprimandRequest<TAction, TResult> : IRequest<TResult>, INotification
        where TAction : ReprimandAction
        where TResult : ReprimandResult
    {
        public ReprimandRequest(IGuildUser user, IGuildUser moderator, TAction reprimand)
        {
            User      = user;
            Reprimand = reprimand;
            Moderator = moderator;
        }

        public IGuildUser Moderator { get; }

        public IGuildUser User { get; }

        public TAction Reprimand { get; }
    }
}