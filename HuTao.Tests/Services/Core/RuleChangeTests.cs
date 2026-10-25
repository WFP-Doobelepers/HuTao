using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Core;
using HuTao.Tests.Testing;
using Moq;
using Xunit;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Tests.Services.Core;

/// <summary>
///     The bot judges every rule with its own authority, so a member may add or remove a rule only for the scopes they
///     have, and only for the members and roles that rank below them. Every command that adds or removes a rule asks
///     <see cref="AuthorizationService.RefuseRuleChangeAsync" /> first.
/// </summary>
public class RuleChangeTests
{
    private const ulong ModeratorId = 100;
    private const int ModeratorRank = 10;

    [Fact]
    public async Task ARuleForAScopeTheModeratorLacks_IsRefused()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.Configuration);

        var refusal = await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, Channel()));

        Assert.Equal("You can only add or remove rules for scopes you have, and you do not have the Ban scope.", refusal);
    }

    [Fact]
    public async Task ARuleForAScopeTheModeratorHas_IsAllowed()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.Configuration | AuthorizationScope.Ban);

        Assert.Null(await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, Channel())));
    }

    [Fact]
    public async Task ACategoryRule_ForAScopeTheModeratorHasInThatCategory_IsAllowed()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.Configuration);
        var raids = fixture.AddCategory("Raids");
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Ban, moderator);

        Assert.Null(await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, Channel()), raids));
    }

    [Fact]
    public async Task ACategoryRule_ForAScopeTheModeratorHasOnlyInAnotherCategory_IsRefused()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.Configuration);
        var raids = fixture.AddCategory("Raids");
        var spam = fixture.AddCategory("Spam");
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Ban, moderator);

        Assert.NotNull(await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, Channel()), spam));
    }

    [Fact]
    public async Task ACategoryRule_ForAScopeTheModeratorHasInTheServer_IsAllowed()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.Configuration | AuthorizationScope.Ban);
        var raids = fixture.AddCategory("Raids");

        Assert.Null(await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, Channel()), raids));
    }

    [Theory]
    [InlineData(ModeratorRank + 10)]
    [InlineData(ModeratorRank)]
    public async Task ARuleForAMemberAtOrAboveTheModerator_IsRefused(int rank)
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.All);
        Member(fixture, 77, rank);

        var refusal = await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, new UserCriterion(77)));

        Assert.Equal("<@77> ranks at or above you, so you cannot add or remove a rule for them.", refusal);
    }

    [Fact]
    public async Task ARuleForAMemberBelowTheModerator_IsAllowed()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.All);
        Member(fixture, 77, ModeratorRank - 5);

        Assert.Null(await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, new UserCriterion(77))));
    }

    [Fact]
    public async Task ARuleForAUserWhoIsNotInTheServer_IsAllowed()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.All);

        Assert.Null(await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, new UserCriterion(77))));
    }

    [Fact]
    public async Task ARuleForARoleAtOrAboveTheModerator_IsRefused()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.All);
        var admin = Role(fixture, 78, "Admin", ModeratorRank);

        var refusal = await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, new RoleCriterion(admin)));

        Assert.Equal("The Admin role ranks at or above your highest role, so you cannot add or remove a rule for it.",
            refusal);
    }

    [Fact]
    public async Task ARuleForARoleBelowTheModerator_IsAllowed()
    {
        var (fixture, moderator) = Moderator(AuthorizationScope.All);
        var helper = Role(fixture, 79, "Helper", ModeratorRank - 5);

        Assert.Null(await RefuseAsync(fixture, moderator, Rule(AuthorizationScope.Ban, new RoleCriterion(helper))));
    }

    [Fact]
    public async Task TheServerOwner_MayChangeAnyRule()
    {
        var fixture = new AuthorizationFixture();
        var owner = fixture.User(ModeratorId);
        Mock.Get(owner).SetupGet(u => u.Hierarchy).Returns(int.MaxValue);
        Mock.Get(fixture.DiscordGuild).SetupGet(g => g.OwnerId).Returns(ModeratorId);
        Member(fixture, 77, ModeratorRank + 10);

        Assert.Null(await RefuseAsync(fixture, owner, Rule(AuthorizationScope.All, new UserCriterion(77))));
    }

    private static (AuthorizationFixture Fixture, IGuildUser Moderator) Moderator(AuthorizationScope scopes)
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(ModeratorId);
        Mock.Get(moderator).SetupGet(u => u.Hierarchy).Returns(ModeratorRank);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, scopes, moderator);
        return (fixture, moderator);
    }

    private static void Member(AuthorizationFixture fixture, ulong id, int rank)
    {
        var member = fixture.User(id);
        Mock.Get(member).SetupGet(u => u.Hierarchy).Returns(rank);
        Mock.Get(member).SetupGet(u => u.Mention).Returns($"<@{id}>");
        Mock.Get(fixture.DiscordGuild)
            .Setup(g => g.GetUserAsync(id, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(member);
    }

    private static IRole Role(AuthorizationFixture fixture, ulong id, string name, int position)
    {
        var role = Mock.Of<IRole>(r => r.Id == id && r.Name == name && r.Position == position
            && r.Guild == fixture.DiscordGuild);
        Mock.Get(fixture.DiscordGuild).Setup(g => g.GetRole(id)).Returns(role);
        return role;
    }

    private static Criterion Channel() => new ChannelCriterion(902, false);

    private static AuthorizationGroup Rule(AuthorizationScope scope, Criterion criterion)
        => new(scope, AccessType.Deny, JudgeType.Any, criterion);

    private static Task<string?> RefuseAsync(
        AuthorizationFixture fixture, IGuildUser moderator, AuthorizationGroup rule, ModerationCategory? category = null)
        => fixture.Auth.RefuseRuleChangeAsync(new CommandContext(fixture.Command(moderator)), moderator, rule, category);
}
