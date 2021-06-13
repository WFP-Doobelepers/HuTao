using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.Core;

namespace Zhongli.Services.Utilities
{
    public class RequireAuthorizationAttribute : PreconditionAttribute
    {
        private readonly AuthorizationScope _scopes = AuthorizationScope.All;

        public RequireAuthorizationAttribute(AuthorizationScope scopes) { _scopes |= scopes; }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            if (context.User is not IGuildUser user)
                return PreconditionResult.FromError("User could not be cast as SocketGuildUser");

            var auth = services.GetRequiredService<AuthorizationService>();
            var isAuthorized = await auth.IsAuthorized(context, user, _scopes);

            return isAuthorized
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You do not have permission to use this command.");
        }
    }
}