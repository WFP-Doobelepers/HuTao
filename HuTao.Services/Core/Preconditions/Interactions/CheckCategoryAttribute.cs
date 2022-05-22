using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using Microsoft.Extensions.DependencyInjection;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.Preconditions.Interactions;

public class CheckCategoryAttribute : ParameterPreconditionAttribute
{
    private readonly AuthorizationScope _scope;

    public CheckCategoryAttribute(AuthorizationScope scope) { _scope = scope | AuthorizationScope.All; }

    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, IParameterInfo parameterInfo,
        object value, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var interaction = new InteractionContext(context);

        var category = value switch
        {
            ModerationCategory c => c,
            ICategory m          => m.Category,
            _                    => ModerationCategory.Default
        };

        return await auth.IsAuthorizedAsync(interaction, _scope, category)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError($"Not authorized to use the `{category?.Name ?? "Default"}` category.");
    }
}