using System.Threading.Tasks;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Tests.Testing;
using Xunit;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Tests.Services.Core;

public class AuthorizationServiceTests
{
    private const AuthorizationScope Scope = AuthorizationScope.All | AuthorizationScope.Modify;

    [Fact]
    public async Task UncategorizedReprimand_IsCheckedAgainstTheGuild_NotTheDefaultCategory()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var moderator = fixture.User(100, defaultCategory: appeals);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.Modify, moderator);

        var context = new InteractionContext(fixture.Interaction(moderator));

        Assert.False(await fixture.Auth.IsReprimandAuthorizedAsync(context, Scope, Reprimand(fixture, null)));
        Assert.True(await fixture.Auth.IsReprimandAuthorizedAsync(context, Scope, Reprimand(fixture, appeals)));

        // Passing the missing category through falls back to the moderator's default, which let them in.
        Assert.True(await fixture.Auth.IsCategoryAuthorizedAsync(context, Scope, (ModerationCategory?) null));
    }

    [Fact]
    public async Task UncategorizedReprimand_AllowsGuildModerators()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Modify, moderator);

        var context = new InteractionContext(fixture.Interaction(moderator));

        Assert.True(await fixture.Auth.IsReprimandAuthorizedAsync(context, Scope, Reprimand(fixture, null)));
        Assert.False(await fixture.Auth.IsReprimandAuthorizedAsync(context, Scope, Reprimand(fixture, appeals)));
    }

    [Fact]
    public async Task CategorizedReprimand_RefusesModeratorsOfOtherCategories()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100, defaultCategory: appeals);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.Modify, moderator);

        var context = new InteractionContext(fixture.Interaction(moderator));

        Assert.False(await fixture.Auth.IsReprimandAuthorizedAsync(context, Scope, Reprimand(fixture, reports)));
    }

    [Fact]
    public async Task WhereReprimandsAuthorized_KeepsTheAllowedOnesInOrder()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.Modify, moderator);

        var first = Reprimand(fixture, appeals);
        var second = Reprimand(fixture, appeals);
        var reprimands = new[] { Reprimand(fixture, reports), first, Reprimand(fixture, null), second };

        var context = new InteractionContext(fixture.Interaction(moderator));
        var allowed = await fixture.Auth.WhereReprimandsAuthorizedAsync(context, Scope, reprimands);

        Assert.Equal(new[] { first, second }, allowed);
    }

    private static Reprimand Reprimand(AuthorizationFixture fixture, ModerationCategory? category)
        => new Note(new ReprimandDetails(fixture.User(400), fixture.User(300), "reason")) { Category = category };
}
