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

public class TemporaryRoleMemberService : ExpirableService<TemporaryRoleMember>
{
    private readonly DiscordSocketClient _client;
    private readonly ZhongliContext _db;

    public TemporaryRoleMemberService(IMemoryCache cache, ZhongliContext db, DiscordSocketClient client)
        : base(cache, db)
    {
        _client = client;
        _db     = db;
    }

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
        var guild = _client.GetGuild(temporary.GuildId);
        var role = guild?.GetRole(temporary.RoleId);
        var user = guild?.GetUser(temporary.UserId);

        if (user is not null && role is not null)
            await user.RemoveRoleAsync(role);
    }
}