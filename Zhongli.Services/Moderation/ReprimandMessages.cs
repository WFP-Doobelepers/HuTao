using MediatR;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Services.Moderation
{
    public record ReprimandRequest<TAction, TResult>(ReprimandDetails Details, TAction Reprimand)
        : IRequest<TResult>
        where TAction : ReprimandAction
        where TResult : ReprimandResult;

    public record ReprimandNotification(ReprimandDetails Details, ReprimandResult Reprimand)
        : INotification;

    public record ModifiedReprimandNotification(ModifiedReprimand Details, ReprimandAction Reprimand)
        : INotification;
}