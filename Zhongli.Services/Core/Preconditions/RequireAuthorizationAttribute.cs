using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Data.Models.Authorization;

namespace Zhongli.Services.Core.Preconditions;

public class RequireAuthorizationAttribute : PreconditionAttribute
{
    private readonly AuthorizationScope _scopes = AuthorizationScope.All;

    public RequireAuthorizationAttribute(AuthorizationScope scopes) { _scopes |= scopes; }

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
        CommandInfo command, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var isAuthorized = await auth.IsAuthorizedAsync(context, _scopes);

        return isAuthorized
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You do not have permission to use this command.");
    }
}