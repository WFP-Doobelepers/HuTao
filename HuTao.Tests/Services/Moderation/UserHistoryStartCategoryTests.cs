using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Xunit;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Tests.Services.Moderation;

/// <summary>
///     History opened without a category used the default category of the user being looked up, usually none, so a
///     moderator scoped to one category saw every reprimand without a category. It must open on a category the
///     requester may view.
/// </summary>
public class UserHistoryStartCategoryTests
{
    [Fact]
    public async Task CategoryModerator_OpensOnTheirDefaultCategory()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var moderator = fixture.User(100, defaultCategory: appeals);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.History, moderator);

        Assert.Same(appeals, await StartCategoryAsync(fixture, moderator));
    }

    [Fact]
    public async Task CategoryModerator_WithoutAUsableDefault_OpensOnTheirFirstCategory()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100, defaultCategory: appeals);
        AuthorizationFixture.Allow(reports.Authorization, AuthorizationScope.History, moderator);

        Assert.Same(reports, await StartCategoryAsync(fixture, moderator));
    }

    [Fact]
    public async Task GuildModerator_WithoutADefault_OpensOnReprimandsWithoutACategory()
    {
        var fixture = new AuthorizationFixture();
        fixture.AddCategory("Appeals");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.History, moderator);

        Assert.Same(ModerationCategory.None, await StartCategoryAsync(fixture, moderator));
    }

    [Fact]
    public async Task GuildModerator_WithADefaultTheyCannotView_OpensOnAll()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var moderator = fixture.User(100, defaultCategory: appeals);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.History, moderator);

        Assert.Same(ModerationCategory.All, await StartCategoryAsync(fixture, moderator));
    }

    [Fact]
    public async Task Member_OpensOnNothing()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.History, fixture.User(100));

        Assert.Null(await StartCategoryAsync(fixture, fixture.User(200)));
    }

    private static Task<ModerationCategory?> StartCategoryAsync(AuthorizationFixture fixture, IGuildUser requester)
        => new UserService(fixture.Auth, null!, null!, fixture.Db)
            .GetStartCategoryAsync(new InteractionContext(fixture.Interaction(requester)), fixture.Guild);
}
