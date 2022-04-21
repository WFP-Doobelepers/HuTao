using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Expirable;

public class TemporaryRoleService : ExpirableService<TemporaryRole>
{
    private readonly DiscordSocketClient _client;
    private readonly HuTaoContext _db;

    public TemporaryRoleService(IMemoryCache cache, HuTaoContext db, DiscordSocketClient client) : base(cache, db)
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