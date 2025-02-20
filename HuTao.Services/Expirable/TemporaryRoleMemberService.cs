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

public class TemporaryRoleMemberService(IMemoryCache cache, HuTaoContext db, DiscordSocketClient client)
    : ExpirableService<TemporaryRoleMember>(cache, db)
{
    private readonly HuTaoContext _db = db;

    public async Task AddTemporaryRoleMemberAsync(IGuildUser user, IRole role, TimeSpan length,
        CancellationToken cancellationToken = default)
    {
        await user.AddRoleAsync(role);

        var guild = await _db.Guilds.TrackGuildAsync(role.Guild, cancellationToken);
        var temporary = new TemporaryRoleMember(user, role, length);

        guild.TemporaryRoleMembers.Add(temporary);
        await _db.SaveChangesAsync(cancellationToken);

        EnqueueExpirableEntity(temporary, cancellationToken);
    }

    protected override async Task OnExpiredEntity(TemporaryRoleMember temporary,
        CancellationToken cancellationToken)
    {
        IGuild guild = client.GetGuild(temporary.GuildId);
        if (guild is null) return;
        
        var role = guild.GetRole(temporary.RoleId);
        var user = await guild.GetUserAsync(temporary.UserId);

        if (user is not null && role is not null)
            await user.RemoveRoleAsync(role);
    }
}