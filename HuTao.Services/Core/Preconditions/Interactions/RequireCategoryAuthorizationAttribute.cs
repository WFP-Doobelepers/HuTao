using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data.Models.Authorization;
using Microsoft.Extensions.DependencyInjection;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.Preconditions.Interactions;

public class RequireCategoryAuthorizationAttribute : PreconditionAttribute
{
    private readonly AuthorizationScope _scopes = AuthorizationScope.All;

    public RequireCategoryAuthorizationAttribute(AuthorizationScope scopes) { _scopes |= scopes; }

    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var isAuthorized = await auth.IsCategoryAuthorizedAsync(new InteractionContext(context), _scopes);

        return isAuthorized
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You do not have permission to use this command.");
    }
}