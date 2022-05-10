using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using static HuTao.Data.Models.Authorization.AuthorizationScope;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.Preconditions.Interactions;

public class RequireEphemeralScopeAttribute : ParameterPreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, IParameterInfo parameter, object value, IServiceProvider services)
    {
        if (value is false) return PreconditionResult.FromSuccess();
        if (context.User is not IGuildUser)
            return PreconditionResult.FromError("This cannot be used outside of a guild.");

        var auth = services.GetRequiredService<AuthorizationService>();
        var authorized = await auth.IsAuthorizedAsync(new InteractionContext(context), All | Ephemeral);
        return value is true && authorized
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You do not have permission to use ephemeral messages.");
    }
}