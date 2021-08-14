using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Hangfire;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core
{
    public class TemporaryRoleService
    {
        private readonly DiscordSocketClient _client;
        private readonly ZhongliContext _db;

        public TemporaryRoleService(DiscordSocketClient client, ZhongliContext db)
        {
            _client = client;
            _db     = db;
        }

        public async Task CreateTemporaryRoleAsync(IRole role, TimeSpan length,
            CancellationToken cancellationToken = default)
        {
            var guild = await _db.Guilds.TrackGuildAsync(role.Guild, cancellationToken);
            var temporary = new TemporaryRole(role, length);

            guild.TemporaryRoles.Add(temporary);
            await _db.SaveChangesAsync(cancellationToken);

            EnqueueExpirableRole(temporary, cancellationToken);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public async Task ExpireRoleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var role = await _db.Set<TemporaryRole>().AsAsyncEnumerable()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            if (role is not null)
                await ExpireRoleAsync(role, cancellationToken);
        }

        public void EnqueueExpirableRole(TemporaryRole expire, CancellationToken cancellationToken = default)
        {
            if (expire.ExpireAt is not null)
            {
                BackgroundJob.Schedule(()
                        => ExpireRoleAsync(expire.Id, cancellationToken),
                    expire.ExpireAt.Value);
            }
        }

        private async Task ExpireRoleAsync(TemporaryRole temporary, CancellationToken cancellationToken = default)
        {
            temporary.EndedAt = DateTimeOffset.Now;

            var role = _client
                .GetGuild(temporary.GuildId)
                ?.GetRole(temporary.RoleId);

            if (role is not null)
                await role.DeleteAsync();

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}