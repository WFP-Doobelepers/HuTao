using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using Xunit;

namespace HuTao.Tests.Services;

// A join arrives together with the member update that follows it, such as the nickname the name censor sets, and each
// event's scope finds the new member missing and adds the member's row. The first save inserts it. The second used
// to fail on the primary key, and the handler lost the rest of its work. On a real PostgreSQL, where the key fires.
// Opt-in: every test no-ops unless HUTAO_PG_RACE=1 and HUTAO_PG_RACE_CS names the scratch database hutao_race_check on
// localhost.
public class PostgresTrackRaceTests
{
    private const string ScratchDatabase = "hutao_race_check";

    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("HUTAO_PG_RACE") == "1"
            ? Environment.GetEnvironmentVariable("HUTAO_PG_RACE_CS")
            : null;

    private static HuTaoContext NewDb(string cs) => new(new DbContextOptionsBuilder<HuTaoContext>().UseNpgsql(cs).Options);

    [Fact]
    public async Task TwoScopesTrackingOneNewMember_BothSave_AndOneRowIsKept()
    {
        if (ConnectionString is not { } cs) return;
        var builder = new NpgsqlConnectionStringBuilder(cs);
        Assert.Equal(ScratchDatabase, builder.Database);
        Assert.Contains(builder.Host, new[] { "localhost", "127.0.0.1", "::1" });

        var guildId = (ulong)Random.Shared.NextInt64(1L << 40, 1L << 60);
        var userId = guildId + 1;
        var guild = Mock.Of<IGuild>(g => g.Id == guildId);
        var user = Mock.Of<IGuildUser>(u => u.Id == userId && u.Guild == guild);
        try
        {
            // Both scopes add the server's row and the member's row before either one saves.
            await using var joined = NewDb(cs);
            await using var updated = NewDb(cs);
            await joined.Guilds.TrackGuildAsync(guild);
            await joined.Users.TrackUserAsync(user);
            await updated.Guilds.TrackGuildAsync(guild);
            await updated.Users.TrackUserAsync(user);

            await joined.SaveChangesAsync();
            await updated.SaveChangesAsync();

            await using var check = NewDb(cs);
            Assert.Equal(1, await check.Users.CountAsync(u => u.Id == userId && u.GuildId == guildId));
            Assert.Equal(1, await check.Guilds.CountAsync(g => g.Id == guildId));
        }
        finally
        {
            await using var db = NewDb(cs);
            await db.Users.Where(u => u.GuildId == guildId).ExecuteDeleteAsync();
            await db.Guilds.Where(g => g.Id == guildId).ExecuteDeleteAsync();
        }
    }
}
