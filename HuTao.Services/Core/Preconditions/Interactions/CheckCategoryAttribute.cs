using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data.Models.Authorization;
using Microsoft.Extensions.DependencyInjection;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.Preconditions.Interactions;

public class CheckCategoryAttribute : ParameterPreconditionAttribute
{
    private readonly AuthorizationScope _scope;

    public CheckCategoryAttribute(AuthorizationScope scope) { _scope = scope | AuthorizationScope.All; }

    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, IParameterInfo parameterInfo,
        object? value, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var interaction = new InteractionContext(context);

        return await auth.IsCategoryAuthorizedAsync(interaction, _scope, value)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You are not authorized to use this moderation category.");
    }
}