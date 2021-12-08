using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;

namespace Zhongli.Services.Expirable;

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