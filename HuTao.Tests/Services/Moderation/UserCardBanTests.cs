using System.Threading.Tasks;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Xunit;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Tests.Services.Moderation;

// The user card shows the latest ban that has no category. A moderator who may view history only in their own category
// learns that the user is banned, but not who banned them or why, the same as their history view.
public class UserCardBanTests
{
    [Fact]
    public async Task CategoryModerator_SeesTheBan_NotItsDetails()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var moderator = fixture.User(100, defaultCategory: appeals);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.History, moderator);

        var (latest, viewable) = await LatestBanAsync(fixture, moderator);

        Assert.NotNull(latest);
        Assert.False(viewable);
    }

    [Fact]
    public async Task GuildModerator_SeesTheBanDetails()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.History, moderator);

        var (latest, viewable) = await LatestBanAsync(fixture, moderator);

        Assert.NotNull(latest);
        Assert.True(viewable);
    }

    private static Task<(Ban? Latest, bool Viewable)> LatestBanAsync(
        AuthorizationFixture fixture, Discord.IGuildUser moderator)
    {
        var banned = new GuildUserEntity(77, fixture.Guild.Id) { Guild = fixture.Guild };
        fixture.Guild.ReprimandHistory.Add(new Ban(0, null, new ReprimandDetails(fixture.User(77), fixture.User(300), "spam")));

        return new UserService(fixture.Auth, null!, null!, fixture.Db)
            .LatestBanAsync(new InteractionContext(fixture.Interaction(moderator)), banned);
    }
}
