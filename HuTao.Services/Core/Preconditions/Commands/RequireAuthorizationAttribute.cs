using System;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Data.Models.Authorization;
using Microsoft.Extensions.DependencyInjection;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Services.Core.Preconditions.Commands;

public class RequireAuthorizationAttribute : PreconditionAttribute
{
    private readonly AuthorizationScope _scopes = AuthorizationScope.All;

    public RequireAuthorizationAttribute(AuthorizationScope scopes) { _scopes |= scopes; }

    public override async Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var isAuthorized = await auth.IsAuthorizedAsync(new CommandContext(context), _scopes);

        return isAuthorized
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You do not have permission to use this command.");
    }
}