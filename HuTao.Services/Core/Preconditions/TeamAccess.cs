using System.Linq;
using Discord;

namespace HuTao.Services.Core.Preconditions;

/// <summary>Who counts as part of the bot's team for the commands that give access to the host.</summary>
public static class TeamAccess
{
    /// <summary>
    ///     Whether <paramref name="userId" /> owns the application, or is a team member who accepted the invite and is
    ///     an admin or a developer. Read-only members and pending invites do not count. An application with no team
    ///     belongs to its owner alone.
    /// </summary>
    public static bool IsTeamMember(IApplication application, ulong userId)
    {
        if (application.Team is not { } team)
            return application.Owner?.Id == userId;

        if (team.OwnerUserId == userId)
            return true;

        return team.TeamMembers.Any(t
            => t.User.Id == userId
            && t.MembershipState is MembershipState.Accepted
            && t.Role is TeamRole.Owner or TeamRole.Admin or TeamRole.Developer);
    }
}
