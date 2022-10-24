using System;
using System.Collections.Generic;
using System.Linq;
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

        return await (value switch
        {
            ModerationCategory c              => CheckCategory(c),
            ICategory m                       => CheckCategory(m.Category),
            ModerationCategory[] c            => CheckCategories(c),
            ICategory[] m                     => CheckCategories(m.Select(c => c.Category)),
            IEnumerable<ModerationCategory> c => CheckCategories(c),
            IEnumerable<ICategory> m          => CheckCategories(m.Select(c => c.Category)),
            _                                 => CheckCategory(ModerationCategory.Default)
        });

        Task<PreconditionResult> CheckCategory(params ModerationCategory?[] category) => CheckCategories(category);

        async Task<PreconditionResult> CheckCategories(IEnumerable<ModerationCategory?> categories)
        {
            foreach (var category in categories)
            {
                if (await auth.IsCategoryAuthorizedAsync(interaction, _scope, category)) continue;

                return PreconditionResult.FromError(
                    $"Not authorized to use the `{category?.Name ?? "Default"}` category.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}