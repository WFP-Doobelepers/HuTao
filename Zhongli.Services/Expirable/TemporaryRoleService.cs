using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Expirable;

public class TemporaryRoleService : ExpirableService<TemporaryRole>
{
    private readonly DiscordSocketClient _client;
    private readonly ZhongliContext _db;

    public TemporaryRoleService(IMemoryCache cache, ZhongliContext db, DiscordSocketClient client) : base(cache, db)
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

        EnqueueExpirableEntity(temporary, cancellationToken);
    }

    protected override async Task OnExpiredEntity(TemporaryRole temporary, CancellationToken cancellationToken)
    {
        var role = _client
            .GetGuild(temporary.GuildId)
            ?.GetRole(temporary.RoleId);

        if (role is not null)
            await role.DeleteAsync();
    }
}