using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
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
        var command = new CommandContext(context);

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
                if (await auth.IsCategoryAuthorizedAsync(command, _scope, category)) continue;

                return PreconditionResult.FromError(
                    $"Not authorized to use the `{category?.Name ?? "Default"}` category.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}