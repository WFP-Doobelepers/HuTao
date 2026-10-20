using System;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Data.Models.Authorization;
using Microsoft.Extensions.DependencyInjection;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Services.Core.Preconditions.Commands;

/// <summary>
///     Requires the scope at the guild level or in any moderation category. For commands that check the category of
///     the record they act on, where the user's default category says nothing about that record.
/// </summary>
public class RequireAnyCategoryAuthorizationAttribute : PreconditionAttribute
{
    private readonly AuthorizationScope _scopes = AuthorizationScope.All;

    public RequireAnyCategoryAuthorizationAttribute(AuthorizationScope scopes) { _scopes |= scopes; }

    public override async Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var auth = services.GetRequiredService<AuthorizationService>();
        var isAuthorized = await auth.IsAuthorizedInAnyAsync(new CommandContext(context), _scopes);

        return isAuthorized
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You do not have permission to use this command.");
    }
}
