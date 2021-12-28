using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Data.Models.Authorization;
using InteractionContext = Zhongli.Data.Models.Discord.InteractionContext;

namespace Zhongli.Services.Core.Preconditions.Interactions;

public class RequireAuthorizationAttribute : PreconditionAttribute
{
    private readonly AuthorizationScope _scopes = AuthorizationScope.All;

    public RequireAuthorizationAttribute(AuthorizationScope scopes) { _scopes |= scopes; }

    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var isAuthorized = await auth.IsAuthorizedAsync(new InteractionContext(context), _scopes);

        return isAuthorized
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You do not have permission to use this command.");
    }
}