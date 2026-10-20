using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Tests.Testing;
using Moq;
using Xunit;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Tests.Services.Core;

/// <summary>
///     Granting a scope writes an authorization rule with the bot's authority, so a member can only grant what they hold.
/// </summary>
public class GrantableScopeTests
{
    [Fact]
    public async Task ConfigurationMember_CannotGrantAll()
    {
        var fixture = new AuthorizationFixture();
        var member = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Configuration, member);

        var unheld = await fixture.Auth.FindUnheldScopeAsync(
            new CommandContext(fixture.Command(member)), member, AuthorizationScope.All);

        Assert.Equal(AuthorizationScope.All, unheld);
    }

    [Fact]
    public async Task ConfigurationMember_CanGrantConfiguration()
    {
        var fixture = new AuthorizationFixture();
        var member = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Configuration, member);

        var unheld = await fixture.Auth.FindUnheldScopeAsync(
            new CommandContext(fixture.Command(member)), member, AuthorizationScope.Configuration);

        Assert.Null(unheld);
    }

    [Fact]
    public async Task CombinedScopes_ReportTheFirstOneTheMemberLacks()
    {
        var fixture = new AuthorizationFixture();
        var member = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Configuration, member);

        var unheld = await fixture.Auth.FindUnheldScopeAsync(
            new CommandContext(fixture.Command(member)), member,
            AuthorizationScope.Configuration | AuthorizationScope.Ban);

        Assert.Equal(AuthorizationScope.Ban, unheld);
    }

    [Fact]
    public async Task MemberWithAll_CanGrantAnything()
    {
        var fixture = new AuthorizationFixture();
        var member = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.All, member);

        var unheld = await fixture.Auth.FindUnheldScopeAsync(
            new CommandContext(fixture.Command(member)), member, AuthorizationScope.All | AuthorizationScope.Ban);

        Assert.Null(unheld);
    }

    [Fact]
    public async Task GuildOwner_CanGrantAnything_WithoutAnyRule()
    {
        var fixture = new AuthorizationFixture();
        var owner = fixture.User(100);
        Mock.Get(fixture.DiscordGuild).SetupGet(g => g.OwnerId).Returns(100);

        var unheld = await fixture.Auth.FindUnheldScopeAsync(
            new CommandContext(fixture.Command(owner)), owner, AuthorizationScope.All);

        Assert.Null(unheld);
    }

    [Fact]
    public async Task DiscordAdministrator_CanGrantAnything_WithoutAnyRule()
    {
        var fixture = new AuthorizationFixture();
        var admin = fixture.User(100);
        Mock.Get(admin).SetupGet(u => u.GuildPermissions).Returns(new GuildPermissions(administrator: true));

        var unheld = await fixture.Auth.FindUnheldScopeAsync(
            new CommandContext(fixture.Command(admin)), admin, AuthorizationScope.All);

        Assert.Null(unheld);
    }
}
