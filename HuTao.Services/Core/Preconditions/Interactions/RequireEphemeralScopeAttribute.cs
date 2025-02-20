using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using static HuTao.Data.Models.Authorization.AuthorizationScope;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.Preconditions.Interactions;

public interface IEphemeral
{
    public bool Ephemeral { get; }
}

public class RequireEphemeralScopeAttribute : ParameterPreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, IParameterInfo parameter, object value, IServiceProvider services)
    {
        if (context.User is not IGuildUser)
            return PreconditionResult.FromError("This cannot be used outside of a guild.");

        var ephemeral = value switch
        {
            bool c       => c,
            IEphemeral m => m.Ephemeral,
            _ => throw new ArgumentException(
                $"{nameof(value)} must be a boolean or implement {nameof(IEphemeral)}")
        };

        if (ephemeral is false) return PreconditionResult.FromSuccess();

        var auth = services.GetRequiredService<AuthorizationService>();
        var authorized = await auth.IsAuthorizedAsync(new InteractionContext(context), All | Ephemeral);
        return authorized
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You do not have permission to use ephemeral messages.");
    }
}