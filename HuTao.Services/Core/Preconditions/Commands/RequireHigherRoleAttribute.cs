using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace HuTao.Services.Core.Preconditions.Commands;

public class RequireHigherRoleAttribute : ParameterPreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
    {
        if (context.User is not IGuildUser user)
            return PreconditionResult.FromError("This command cannot be used outside of a guild.");

        var bot = await context.Guild.GetCurrentUserAsync().ConfigureAwait(false);
        return value switch
        {
            IEnumerable<IGuildUser> users => CheckUsers(users),
            IGuildUser target => target.Hierarchy >= bot.Hierarchy
                ? PreconditionResult.FromError($"The bot's role is lower than {target}.")
                : target.Hierarchy >= user.Hierarchy
                    ? PreconditionResult.FromError($"You cannot {parameter.Command.Name} {target}.")
                    : PreconditionResult.FromSuccess(),
            IEnumerable<IUser> => PreconditionResult.FromSuccess(),
            IUser              => PreconditionResult.FromSuccess(),
            _                  => PreconditionResult.FromError("Invalid parameter type.")
        };

        PreconditionResult CheckUsers(IEnumerable<IGuildUser> users)
        {
            foreach (var u in users)
            {
                if (u.Hierarchy >= user.Hierarchy)
                    return PreconditionResult.FromError($"You cannot {parameter.Command.Name} {u}.");

                if (u.Hierarchy >= bot.Hierarchy)
                    return PreconditionResult.FromError($"The bot's role is lower than {u}.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}