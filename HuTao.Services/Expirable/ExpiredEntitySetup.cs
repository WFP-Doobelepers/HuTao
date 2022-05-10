using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core.Messages;
using HuTao.Services.Moderation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Expirable;

public static class ExpiredEntitySetup
{
    public static IServiceCollection AddExpirableServices(this IServiceCollection services)
        => services
            .AddExpirableService<ExpirableReprimand, ModerationService>()
            .AddExpirableService<TemporaryRoleMember, TemporaryRoleMemberService>()
            .AddExpirableService<TemporaryRole, TemporaryRoleService>();

    private static IServiceCollection AddExpirableService<TExpirable, TService>(
        this IServiceCollection services)
        where TExpirable : class, IExpirable
        where TService : ExpirableService<TExpirable>
        => services
            .AddScoped<TService>()
            .AddScoped<ExpirableService<TExpirable>, TService>()
            .AddScoped<INotificationHandler<ReadyNotification>, ExpiredEntityBehavior<TExpirable>>();
}