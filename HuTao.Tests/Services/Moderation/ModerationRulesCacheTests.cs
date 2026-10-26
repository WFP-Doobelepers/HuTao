using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

/// <summary>
///     The moderation rules stay in the cache for a minute, and the messages of that minute read them from their own
///     scopes. When a filter fires, the bot reads the reprimand the filter gives. A reprimand that is loaded only when
///     first read would load on the scope that cached the rules, which is disposed by then, so the filter would delete
///     the messages and never give its mute or warning.
/// </summary>
public class ModerationRulesCacheTests
{
    private const ulong GuildId = 801;

    [Fact]
    public async Task CachedRules_CarryEveryTriggersReprimand_AfterTheirScopeIsDisposed()
    {
        var options = new DbContextOptionsBuilder<HuTaoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseLazyLoadingProxies()
            .Options;

        await using (var seed = new HuTaoContext(options))
        {
            var rules = new ModerationRules();
            rules.Triggers.Add(new Censor("(?i)badword", new WarningAction(1), null));
            rules.Triggers.Add(new MentionConfiguration(new MuteAction(TimeSpan.FromMinutes(10)), Mock.Of<IAutoConfigurationOptions>()));
            rules.Triggers.Add(new MessageConfiguration(new TimeoutAction(TimeSpan.FromMinutes(5)), Mock.Of<IAutoConfigurationOptions>()));
            rules.Triggers.Add(new ReprimandTrigger(null, TriggerSource.Warning, new NoticeAction()));
            seed.Add(new GuildEntity(GuildId) { ModerationRules = rules });
            await seed.SaveChangesAsync();
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        ModerationRules? cached;
        await using (var firstScope = new HuTaoContext(options))
            cached = await firstScope.Guilds.GetRulesAsync(Mock.Of<IGuild>(g => g.Id == GuildId), cache);

        // The entities are lazy-loading proxies, so each reprimand's type is named by its base type.
        Assert.NotNull(cached);
        Assert.Equal(
            [nameof(WarningAction), nameof(MuteAction), nameof(TimeoutAction), nameof(NoticeAction)],
            cached.Triggers.OfType<ITriggerAction>()
                .OrderBy(t => t is Censor ? 0 : t is MentionConfiguration ? 1 : t is MessageConfiguration ? 2 : 3)
                .Select(t => t.Reprimand?.GetType().BaseType?.Name));
    }
}
