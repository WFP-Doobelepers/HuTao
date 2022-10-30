using System;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Data.Models.Authorization;
using Microsoft.Extensions.DependencyInjection;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Services.Core.Preconditions.Commands;

public class CheckCategoryAttribute : ParameterPreconditionAttribute
{
    private readonly AuthorizationScope _scope;

    public CheckCategoryAttribute(AuthorizationScope scope) { _scope = scope | AuthorizationScope.All; }

    public override async Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, ParameterInfo parameter,
        object? value, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var command = new CommandContext(context);

        return await auth.IsCategoryAuthorizedAsync(command, _scope, value)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You are not authorized to use this moderation category.");
    }
}