using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Authorization.GuildPermission;

namespace Zhongli.Services.Core
{
    public class AuthorizationService
    {
        private readonly DiscordSocketClient _client;
        private readonly ZhongliContext _db;

        public AuthorizationService(ZhongliContext db, DiscordSocketClient client)
        {
            _db     = db;
            _client = client;
        }

        public async Task<AuthorizationRules> AutoConfigureGuild(ulong guildId,
            CancellationToken cancellationToken = default)
        {
            var guild = await _db.Guilds.FindByIdAsync(guildId, cancellationToken) ??
                _db.Add(new GuildEntity(guildId)).Entity;

            if (guild.AuthorizationRules is not null)
                return guild.AuthorizationRules;

            guild.AuthorizationRules = new AuthorizationRules
            {
                PermissionAuthorizations = new List<PermissionAuthorization>
                {
                    new()
                    {
                        Guild      = guild,
                        AddedById  = _client.CurrentUser.Id,
                        Date       = DateTimeOffset.UtcNow,
                        Permission = GuildPermission.Administrator,
                        Scope      = AuthorizationScope.All
                    }
                }
            };

            await _db.SaveChangesAsync(cancellationToken);

            return guild.AuthorizationRules;
        }

        public async Task<bool> IsAuthorized(
            ICommandContext context, IGuildUser user, AuthorizationScope scope,
            CancellationToken cancellationToken = default)
        {
            var rules = await AutoConfigureGuild(user.GuildId, cancellationToken);

            var isUserAuthorized = ScopedAuthorization(rules.UserAuthorizations)
                .Any(auth => auth.GuildId == user.GuildId && auth.UserId == user.Id);
            if (isUserAuthorized)
                return true;

            var isRoleAuthorized = ScopedAuthorization(rules.RoleAuthorizations)
                .Any(auth => user.RoleIds.Contains(auth.RoleId));
            if (isRoleAuthorized)
                return true;

            var isPermissionsAuthorized = ScopedAuthorization(rules.PermissionAuthorizations)
                .Any(auth => (auth.Permission & (GuildPermission) user.GuildPermissions.RawValue) != 0);
            if (isPermissionsAuthorized)
                return true;

            var isChannelAuthorized = ScopedAuthorization(rules.ChannelAuthorizations)
                .Any(auth => auth.ChannelId == context.Channel.Id);
            if (isChannelAuthorized)
                return true;

            var isGuildAuthorized = ScopedAuthorization(rules.GuildAuthorizations)
                .Any(auth => auth.GuildId == user.GuildId);
            if (isGuildAuthorized)
                return true;

            return false;

            IEnumerable<T> ScopedAuthorization<T>(IEnumerable<T> rule) where T : AuthorizationRule
            {
                return rule.Where(auth => (auth.Scope & scope) != 0);
            }
        }
    }
}