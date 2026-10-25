using System;
using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Xunit;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;
using Timeout = HuTao.Data.Models.Moderation.Infractions.Reprimands.Timeout;

namespace HuTao.Tests.Services.Moderation;

// The buttons of the mute list and the timeout list act on a record by the ID in their custom ID, and the list can be
// older than the record. A button acts only on a record that is still running, and only for a moderator who has the
// scope in the record's category, which is the check the command that made the record ran.
public class RunningReprimandAccessTests
{
    private const ulong ModeratorId = 100;
    private const ulong MemberId = 77;

    // The moderator who made the record, who is not the one pressing the button.
    private const ulong AuthorId = 300;

    [Fact]
    public async Task AServerWideModerator_IsRefusedAMuteOfACategoryWithNoRuleForThem()
    {
        var fixture = new AuthorizationFixture();
        var spam = fixture.AddCategory("Spam");
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Mute, moderator);

        Assert.Equal("You do not have the Mute scope in the Spam category.",
            await RefuseAsync(fixture, moderator, AuthorizationScope.Mute, Mute(fixture, spam)));
    }

    [Fact]
    public async Task AModeratorWithTheScopeOnlyInTheMutesCategory_MayActOnIt()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Mute, moderator);

        Assert.Null(await RefuseAsync(fixture, moderator, AuthorizationScope.Mute, Mute(fixture, raids)));
    }

    [Fact]
    public async Task AModeratorWithTheScopeInAnotherCategory_IsRefused()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var spam = fixture.AddCategory("Spam");
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Mute, moderator);

        Assert.NotNull(await RefuseAsync(fixture, moderator, AuthorizationScope.Mute, Mute(fixture, spam)));
    }

    [Fact]
    public async Task AMuteWithNoCategory_IsCheckedInTheServer_NotInTheModeratorsDefaultCategory()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var moderator = fixture.User(ModeratorId, raids);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Mute, moderator);

        Assert.Null(await RefuseAsync(fixture, moderator, AuthorizationScope.Mute, Mute(fixture, null)));
    }

    [Fact]
    public async Task AMuteThatAModeratorEnded_IsRefused_BeforeItsEndTime()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Mute, moderator);

        var mute = Mute(fixture, null);
        mute.Status = ReprimandStatus.Pardoned;
        mute.EndedAt = DateTimeOffset.UtcNow;

        Assert.Equal("This mute has already ended.",
            await RefuseAsync(fixture, moderator, AuthorizationScope.Mute, mute));
    }

    [Fact]
    public async Task AMuteThatRanOut_IsRefused()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Mute, moderator);

        var mute = Mute(fixture, null);
        mute.EndedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        mute.ExpireAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        Assert.Equal("This mute has already ended.",
            await RefuseAsync(fixture, moderator, AuthorizationScope.Mute, mute));
    }

    [Fact]
    public async Task ATimeout_NeedsTheTimeoutScope()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Mute, moderator);

        var timeout = new Timeout(TimeSpan.FromHours(1),
            new ReprimandDetails(fixture.User(MemberId), fixture.User(AuthorId), "spam"));

        Assert.Equal("You do not have the Timeout scope.",
            await RefuseAsync(fixture, moderator, AuthorizationScope.Timeout, timeout));
    }

    // The lookup by ID loads the record's category with it.
    private static Mute Mute(AuthorizationFixture fixture, ModerationCategory? category)
        => new(TimeSpan.FromHours(1),
            new ReprimandDetails(fixture.User(MemberId), fixture.User(AuthorId), "spam") { Category = category })
        {
            Category = category
        };

    private static Task<string?> RefuseAsync(
        AuthorizationFixture fixture, IGuildUser moderator, AuthorizationScope scope, ExpirableReprimand reprimand)
        => fixture.Auth.RefuseActAsync(new InteractionContext(fixture.Interaction(moderator)), scope, reprimand);
}
