using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Tests.Testing;
using Xunit;
using Commands = HuTao.Services.Core.Preconditions.Commands;
using Interactions = HuTao.Services.Core.Preconditions.Interactions;

namespace HuTao.Tests.Services.Core.Preconditions;

/// <summary>
///     The reprimand commands check the reprimand's own category in their body, so their precondition must let in a
///     moderator whose rights come only from a category, whatever their default category is.
/// </summary>
public class RequireAnyCategoryAuthorizationAttributeTests
{
    private const AuthorizationScope Scope = AuthorizationScope.History | AuthorizationScope.Modify;

    [Fact]
    public async Task CategoryModerator_WithoutADefaultCategory_Passes()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.Modify, moderator);

        Assert.True(await PassesInteraction(fixture, moderator));
        Assert.True(await PassesCommand(fixture, moderator));

        // The default-category check refuses them, which is why it cannot guard these commands.
        var defaultCategory = await new Interactions.RequireCategoryAuthorizationAttribute(Scope)
            .CheckRequirementsAsync(fixture.Interaction(moderator), null!, fixture.Services);
        Assert.False(defaultCategory.IsSuccess);
    }

    [Fact]
    public async Task CategoryModerator_WithAnotherDefaultCategory_Passes()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100, defaultCategory: reports);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.History, moderator);

        Assert.True(await PassesInteraction(fixture, moderator));
        Assert.True(await PassesCommand(fixture, moderator));
    }

    [Fact]
    public async Task GuildModerator_Passes()
    {
        var fixture = new AuthorizationFixture();
        fixture.AddCategory("Appeals");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Modify, moderator);

        Assert.True(await PassesInteraction(fixture, moderator));
        Assert.True(await PassesCommand(fixture, moderator));
    }

    [Fact]
    public async Task Member_WithNoRightsAnywhere_Fails()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var member = fixture.User(200, defaultCategory: appeals);

        // Rights held by someone else, and rights in a scope these commands do not use.
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.Modify, fixture.User(100));
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.Warning, member);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Mute, member);

        Assert.False(await PassesInteraction(fixture, member));
        Assert.False(await PassesCommand(fixture, member));
    }

    private static async Task<bool> PassesInteraction(AuthorizationFixture fixture, IGuildUser user)
    {
        var result = await new Interactions.RequireAnyCategoryAuthorizationAttribute(Scope)
            .CheckRequirementsAsync(fixture.Interaction(user), null!, fixture.Services);

        return result.IsSuccess;
    }

    private static async Task<bool> PassesCommand(AuthorizationFixture fixture, IGuildUser user)
    {
        var result = await new Commands.RequireAnyCategoryAuthorizationAttribute(Scope)
            .CheckPermissionsAsync(fixture.Command(user), null!, fixture.Services);

        return result.IsSuccess;
    }
}
