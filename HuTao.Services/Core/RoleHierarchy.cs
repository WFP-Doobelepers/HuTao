using System.Collections.Generic;
using System.Linq;
using Discord;
using Humanizer;

namespace HuTao.Services.Core;

/// <summary>
///     Discord only lets a member give roles below their own highest role, and only permissions they hold, but the bot
///     gives roles with its own permissions. Anything that makes the bot give or take a role on a member's behalf must
///     make the same check, the way <see cref="Preconditions.Commands.RequireHierarchyAttribute" /> does for a command
///     parameter. The guild owner has a <see cref="IGuildUser.Hierarchy" /> of <see cref="int.MaxValue" />.
/// </summary>
public static class RoleHierarchy
{
    /// <summary>Returns an error for the first role at or above the invoker's highest role, or null.</summary>
    public static string? CheckRoles(IGuildUser invoker, params IEnumerable<IRole>?[] roles)
        => roles
            .Where(group => group is not null)
            .SelectMany(group => group!)
            .Select(role => CheckRole(invoker, role))
            .FirstOrDefault(error => error is not null);

    public static string? CheckRole(IGuildUser invoker, IRole role)
        => role.Position >= invoker.Hierarchy ? $"{role} is higher or equal than your roles." : null;

    /// <summary>
    ///     Returns an error when the role cannot be a mute role: @everyone, a managed role and the bot's own roles would
    ///     have their overwrites changed in every channel, and the invoker must outrank the role like any other.
    /// </summary>
    public static string? CheckMuteRole(IGuildUser invoker, IRole role, IGuildUser bot)
    {
        if (role.Id == invoker.GuildId)
            return "@everyone cannot be used as a mute role.";

        if (role.IsManaged)
            return $"{role} is managed by an integration and cannot be used as a mute role.";

        if (bot.RoleIds.Contains(role.Id))
            return $"{role} is one of the bot's roles and cannot be used as a mute role.";

        return CheckRole(invoker, role);
    }

    /// <summary>
    ///     Returns an error when the requested permissions include one the invoker lacks, or null. The guild owner and
    ///     administrators may request any permission.
    /// </summary>
    public static string? CheckPermissions(IGuildUser invoker, GuildPermissions requested)
    {
        if (invoker.Guild?.OwnerId == invoker.Id || invoker.GuildPermissions.Administrator)
            return null;

        var missing = requested.ToList().Where(p => !invoker.GuildPermissions.Has(p)).ToList();
        return missing.Count == 0
            ? null
            : $"You cannot give a role permissions you do not have: {missing.Humanize(p => p.Humanize())}.";
    }
}
