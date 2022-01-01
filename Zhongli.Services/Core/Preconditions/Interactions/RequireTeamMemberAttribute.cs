using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace Zhongli.Services.Core.Preconditions.Interactions;

public class RequireTeamMemberAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        if (context.Client.TokenType is not TokenType.Bot)
        {
            return PreconditionResult.FromError(
                $"{nameof(RequireTeamMemberAttribute)} is not supported by this TokenType.");
        }

        var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

        if (context.User.Id == application.Owner.Id
            || application.Team.OwnerUserId == application.Owner.Id
            || application.Team.TeamMembers.Any(t => context.User.Id == t.User.Id))
            return PreconditionResult.FromSuccess();

        return PreconditionResult.FromError("Command can only be run by team members of the bot.");
    }
}