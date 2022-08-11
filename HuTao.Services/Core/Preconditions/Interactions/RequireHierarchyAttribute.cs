using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace HuTao.Services.Core.Preconditions.Interactions;

public class RequireHierarchyAttribute : ParameterPreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, IParameterInfo parameterInfo, object value, IServiceProvider services)
    {
        if (context.User is not IGuildUser user)
            return Task.FromResult(PreconditionResult.FromError("This command cannot be used outside of a guild."));

        return value switch
        {
            IEnumerable<IRole> roles => Task.FromResult(CheckHierarchy(roles)),
            IRole role => role.Position >= user.Hierarchy
                ? Task.FromResult(PreconditionResult.FromError("This role is higher or equal than your roles."))
                : Task.FromResult(PreconditionResult.FromSuccess()),
            _ => Task.FromResult(PreconditionResult.FromError("Role not found."))
        };

        PreconditionResult CheckHierarchy(IEnumerable<IRole> roles)
        {
            foreach (var role in roles.Where(role => role.Position >= user.Hierarchy))
            {
                return PreconditionResult.FromError($"{role} is higher or equal than your roles.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}