using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Services.Core
{
    public class AuthorizationService
    {
        private readonly ZhongliContext _db;

        public AuthorizationService(ZhongliContext db) { _db = db; }

        public async Task<GuildEntity> AutoConfigureGuild(IGuild guild,
            CancellationToken cancellationToken = default)
        {
            var guildEntity = await GetGuildAsync(guild, cancellationToken);
            var auth = guildEntity.AuthorizationGroups;

            if (auth.Any()) return guildEntity;
            
            var permission = new PermissionCriterion(GuildPermission.Administrator);
            auth.AddRules(AuthorizationScope.All, await guild.GetCurrentUserAsync(), permission);
            await _db.SaveChangesAsync(cancellationToken);

            return guildEntity;
        }

        private async Task<GuildEntity> GetGuildAsync(IGuild guild, CancellationToken cancellationToken = default)
        {
            var guildEntity = await _db.Guilds.TrackGuildAsync(guild, cancellationToken);
            await _db.Users.TrackUserAsync(await guild.GetCurrentUserAsync(), cancellationToken);

            return guildEntity;
        }

        public async Task<bool> IsAuthorized(
            ICommandContext context, IGuildUser user, AuthorizationScope scope,
            CancellationToken cancellationToken = default)
        {
            var rules = await AutoConfigureGuild(user.Guild, cancellationToken);
            var groups = rules.AuthorizationGroups;

            return groups.Scoped(scope).Any(g => g.Judge(context, user));
        }
    }
}