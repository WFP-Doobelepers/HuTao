using System;
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

        var category = value switch
        {
            ModerationCategory c => c,
            ICategory m          => m.Category,
            _                    => ModerationCategory.Default
        };

        return await auth.IsAuthorizedAsync(command, _scope, category)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError($"Not authorized to use the `{category?.Name ?? "Default"}` category.");
    }
}