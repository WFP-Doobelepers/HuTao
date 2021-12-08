using MediatR;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Services.Moderation;

public record ReprimandRequest<TAction>(ReprimandDetails Details, TAction Reprimand)
    : IRequest<ReprimandResult>
    where TAction : Reprimand;