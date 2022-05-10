using System;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using Microsoft.Extensions.DependencyInjection;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Services.Core.Preconditions.Commands;

public class CheckCategoryAttribute : ParameterPreconditionAttribute
{
    private readonly AuthorizationScope _scope;

    public CheckCategoryAttribute(AuthorizationScope scope) { _scope = scope | AuthorizationScope.All; }

    public override async Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, ParameterInfo parameter,
        object value, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var interaction = new CommandContext(context);

        return value is ModerationCategory category && category != ModerationCategory.None
            ? AuthorizationService.IsAuthorized(interaction, _scope, category)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"You are not authorized to use the `{category.Name}` category.")
            : await auth.IsAuthorizedAsync(interaction, _scope)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You are not authorized to use a blank category.");
    }
}