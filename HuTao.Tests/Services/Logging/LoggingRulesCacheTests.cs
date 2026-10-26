using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Logging;

/// <summary>
///     The logging rules sit in the cache for a minute and the messages that arrive in that minute read them from their
///     own scopes. A rule that loads its channels only when first read would load them on the scope that cached it, which
///     is disposed or busy by then, and the message would go unsaved.
/// </summary>
public class LoggingRulesCacheTests
{
    private const ulong GuildId = 800;

    [Fact]
    public async Task CachedRules_CarryTheirChannelsAndExclusions_AfterTheirScopeIsDisposed()
    {
        var options = new DbContextOptionsBuilder<HuTaoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseLazyLoadingProxies()
            .Options;

        await using (var seed = new HuTaoContext(options))
        {
            var rules = new LoggingRules();
            rules.LoggingChannels.Add(new EnumChannel<LogType>(LogType.MessageDeleted, Mock.Of<IChannel>(c => c.Id == 5)));
            rules.LoggingExclusions.Add(new ChannelCriterion(6, false));
            seed.Add(new GuildEntity(GuildId) { LoggingRules = rules });
            await seed.SaveChangesAsync();
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var guild = Mock.Of<IGuild>(g => g.Id == GuildId);

        LoggingRules? cached;
        await using (var firstScope = new HuTaoContext(options))
            cached = await firstScope.Guilds.GetLoggingAsync(guild, cache);

        Assert.NotNull(cached);
        Assert.Contains(cached.LoggingChannels, c => c.Type == LogType.MessageDeleted);
        Assert.Single(cached.LoggingExclusions);
    }

    [Fact]
    public async Task GuildWithoutRules_CachesNothing()
    {
        var options = new DbContextOptionsBuilder<HuTaoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseLazyLoadingProxies()
            .Options;

        await using (var seed = new HuTaoContext(options))
        {
            seed.Add(new GuildEntity(GuildId));
            await seed.SaveChangesAsync();
        }

        await using var db = new HuTaoContext(options);
        var cached = await db.Guilds.GetLoggingAsync(
            Mock.Of<IGuild>(g => g.Id == GuildId), new MemoryCache(new MemoryCacheOptions()));

        Assert.Null(cached);
        Assert.Empty(db.ChangeTracker.Entries<GuildEntity>().Where(e => e.State == EntityState.Added));
    }
}
