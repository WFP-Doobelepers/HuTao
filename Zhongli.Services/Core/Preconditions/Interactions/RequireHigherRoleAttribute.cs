using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace Zhongli.Services.Core.Preconditions.Interactions;

public class RequireHigherRoleAttribute : ParameterPreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, IParameterInfo parameter, object value, IServiceProvider services)
    {
        if (context.User is not IGuildUser user)
            return PreconditionResult.FromError("This command cannot be used outside of a guild.");

        if (value is not IGuildUser target)
        {
            return value is IUser
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("User not found.");
        }

        if (user.Hierarchy <= target.Hierarchy)
            return PreconditionResult.FromError($"You cannot {parameter.Command.Name} this user.");

        var bot = await context.Guild.GetCurrentUserAsync().ConfigureAwait(false);
        return bot?.Hierarchy <= target.Hierarchy
            ? PreconditionResult.FromError("The bot's role is lower than the targeted user.")
            : PreconditionResult.FromSuccess();
    }
}