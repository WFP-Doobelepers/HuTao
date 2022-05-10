using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace HuTao.Services.Core.Preconditions.Commands;

public class RequireHierarchyAttribute : ParameterPreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
    {
        if (context.User is not IGuildUser user)
            return Task.FromResult(PreconditionResult.FromError("This command cannot be used outside of a guild."));

        return value switch
        {
            SocketRole[] roles            => Task.FromResult(CheckHierarchy(roles, user)),
            IRole[] roles                 => Task.FromResult(CheckHierarchy(roles, user)),
            IEnumerable<SocketRole> roles => Task.FromResult(CheckHierarchy(roles, user)),
            IEnumerable<IRole> roles      => Task.FromResult(CheckHierarchy(roles, user)),
            IRole role => role.Position >= user.Hierarchy
                ? Task.FromResult(PreconditionResult.FromError("This role is higher or equal than your roles."))
                : Task.FromResult(PreconditionResult.FromSuccess()),
            _ => Task.FromResult(PreconditionResult.FromError("Role not found."))
        };
    }

    private static PreconditionResult CheckHierarchy(IEnumerable<IRole> roles, IGuildUser user)
    {
        foreach (var role in roles.Where(role => role.Position >= user.Hierarchy))
        {
            return PreconditionResult.FromError($"{role} is higher or equal than your roles.");
        }

        return PreconditionResult.FromSuccess();
    }
}