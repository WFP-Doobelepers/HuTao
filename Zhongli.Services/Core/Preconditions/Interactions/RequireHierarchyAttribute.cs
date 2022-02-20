using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace Zhongli.Services.Core.Preconditions.Interactions;

public class RequireHierarchyAttribute : ParameterPreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context, IParameterInfo parameterInfo, object value, IServiceProvider services)
    {
        if (context.User is not IGuildUser user)
            return Task.FromResult(PreconditionResult.FromError("This command cannot be used outside of a guild."));

        if (value is not IRole role)
            return Task.FromResult(PreconditionResult.FromError("Role not found."));

        return role.Position >= user.Hierarchy
            ? Task.FromResult(PreconditionResult.FromError("This role is higher or equal than your roles."))
            : Task.FromResult(PreconditionResult.FromSuccess());
    }
}