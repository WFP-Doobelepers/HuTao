using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using GuildPermission = Zhongli.Data.Models.Authorization.GuildPermission;

namespace Zhongli.Services.Core
{
    public class AuthorizationService
    {
        private readonly ZhongliContext _db;

        public AuthorizationService(ZhongliContext db) { _db = db; }

        public async Task<bool> IsAuthorized(
            ICommandContext context, IGuildUser user, AuthorizationScope scope)
        {
            await AutoConfigureGuild(user.GuildId);

            var isUserAuthorized = GetAuthorizations<UserAuthorization>(scope)
                .Any(auth => auth.GuildId == user.GuildId && auth.UserId == user.Id);
            if (isUserAuthorized)
                return true;

            var isRoleAuthorized = GetAuthorizations<RoleAuthorization>(scope)
                .Any(auth => user.RoleIds.Contains(auth.RoleId));
            if (isRoleAuthorized)
                return true;

            var isPermissionsAuthorized = GetAuthorizations<PermissionAuthorization>(scope)
                .Any(auth => (auth.Permission & (GuildPermission) user.GuildPermissions.RawValue) != 0);
            if (isPermissionsAuthorized)
                return true;

            var isChannelAuthorized = GetAuthorizations<ChannelAuthorization>(scope)
                .Any(auth => auth.ChannelId == context.Channel.Id);
            if (isChannelAuthorized)
                return true;

            var isGuildAuthorized = GetAuthorizations<GuildAuthorization>(scope)
                .Any(auth => auth.GuildId == user.GuildId);
            if (isGuildAuthorized)
                return true;

            return false;

            IQueryable<T> GetAuthorizations<T>(AuthorizationScope flags) where T : class, IAuthorizationRule
            {
                return _db.Set<T>().AsQueryable()
                    .Where(auth => (auth.Scope & flags) != 0);
            }
        }

        public async Task AutoConfigureGuild(ulong guildId)
        {
            var guild = await _db.Guilds.FindAsync(guildId);

            if (guild.AuthorizationRules is not null)
                return;

            guild.AuthorizationRules = new AuthorizationRules
            {
                PermissionAuthorizations = new List<PermissionAuthorization>
                {
                    new()
                    {
                        Date       = DateTimeOffset.UtcNow,
                        Permission = GuildPermission.Administrator,
                        Scope      = AuthorizationScope.All
                    }
                }
            };

            await _db.SaveChangesAsync();
        }
    }
}