using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Authorization.GuildPermission;

namespace Zhongli.Services.Core
{
    public class AuthorizationService
    {
        private readonly ZhongliContext _db;

        public AuthorizationService(ZhongliContext db) { _db = db; }

        public async Task<AuthorizationRules> AutoConfigureGuild(IGuild guild,
            CancellationToken cancellationToken = default)
        {
            var guildEntity = await GetGuildAsync(guild, cancellationToken);

            guildEntity.AuthorizationRules ??= new AuthorizationRules
            {
                PermissionAuthorizations = new List<PermissionAuthorization>
                {
                    new(AuthorizationScope.All, await guild.GetCurrentUserAsync(), GuildPermission.Administrator)
                }
            };
            await _db.SaveChangesAsync(cancellationToken);

            return guildEntity.AuthorizationRules;
        }

        private async Task<GuildEntity> GetGuildAsync(IGuild guild, CancellationToken cancellationToken = default)
        {
            var guildEntity = await _db.Guilds.FindByIdAsync(guild.Id, cancellationToken) ??
                _db.Add(new GuildEntity(guild.Id)).Entity;

            await _db.Users.TrackUserAsync(await guild.GetCurrentUserAsync(), cancellationToken);

            return guildEntity;
        }

        public async Task<bool> IsAuthorized(
            ICommandContext context, IGuildUser user, AuthorizationScope scope,
            CancellationToken cancellationToken = default)
        {
            var rules = await AutoConfigureGuild(user.Guild, cancellationToken);
            var groups = rules.AuthorizationGroups;

            return groups.Scoped(scope).All(g => g.IsAuthorized(context, user))
                && rules.Scoped(scope).Any(r => r.IsAuthorized(context, user));
        }
    }
}