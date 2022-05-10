using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using MediatR;

namespace HuTao.Services.Moderation;

public record ReprimandRequest<TAction>(ReprimandDetails Details, TAction Reprimand)
    : IRequest<ReprimandResult>
    where TAction : Reprimand;