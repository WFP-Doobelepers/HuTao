using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace HuTao.Services.Core.Preconditions.Commands;

public class RequireTeamMemberAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (context.Client.TokenType is not TokenType.Bot)
        {
            return PreconditionResult.FromError(
                $"{nameof(RequireTeamMemberAttribute)} is not supported by this TokenType.");
        }

        var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

        if (context.User.Id == application.Owner.Id
            || context.User.Id == application.Team.OwnerUserId
            || application.Team.TeamMembers.Any(t => context.User.Id == t.User.Id))
            return PreconditionResult.FromSuccess();

        return PreconditionResult.FromError("Command can only be run by team members of the bot.");
    }
}