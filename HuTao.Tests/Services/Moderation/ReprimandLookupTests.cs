using System;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Xunit;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Tests.Services.Moderation;

/// <summary>
///     A partial reprimand ID that matches several reprimands opens a selection showing each of them, before the
///     command checks the chosen reprimand's category. So the selection must only offer the reprimands the user may see.
/// </summary>
public class ReprimandLookupTests
{
    private const AuthorizationScope Scope = AuthorizationScope.All | AuthorizationScope.Modify;

    // Without an InteractiveService, opening a selection throws, which proves none was opened.
    private static readonly InteractiveService NoSelection = null!;

    [Fact]
    public async Task PartialId_SkipsTheSelection_WhenOnlyOneMatchIsAllowed()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.Modify, moderator);

        var allowed = Reprimand(fixture, "ab000000-0000-0000-0000-000000000001", appeals);
        Reprimand(fixture, "ab000000-0000-0000-0000-000000000002", reports);
        Reprimand(fixture, "ab000000-0000-0000-0000-000000000003", null);

        Assert.Same(allowed, await FindAsync(fixture, moderator, "ab"));
    }

    [Fact]
    public async Task PartialId_ReturnsAHiddenMatchUnshown_WhenNoneAreAllowed()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.Modify, moderator);

        Reprimand(fixture, "cd000000-0000-0000-0000-000000000001", reports);
        Reprimand(fixture, "cd000000-0000-0000-0000-000000000002", reports);

        // The command's own check then refuses it with a permission error rather than "not found".
        var found = await FindAsync(fixture, moderator, "cd");
        Assert.NotNull(found);
        Assert.False(await fixture.Auth.IsReprimandAuthorizedAsync(Context(fixture, moderator), Scope, found));
    }

    [Fact]
    public async Task ShortOrUnknownIds_FindNothing()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Modify, moderator);
        Reprimand(fixture, "ef000000-0000-0000-0000-000000000001", null);

        Assert.Null(await FindAsync(fixture, moderator, "e"));
        Assert.Null(await FindAsync(fixture, moderator, "99"));
    }

    private static Task<Reprimand?> FindAsync(AuthorizationFixture fixture, IGuildUser user, string id)
        => NoSelection.TryFindReprimandAsync(
            fixture.Auth, Context(fixture, user), fixture.Guild.ReprimandHistory, Scope, id);

    private static InteractionContext Context(AuthorizationFixture fixture, IGuildUser user)
        => new(fixture.Interaction(user));

    private static Reprimand Reprimand(AuthorizationFixture fixture, string id, ModerationCategory? category)
    {
        var reprimand = new Note(new ReprimandDetails(fixture.User(400), fixture.User(300), "reason"))
        {
            Id = Guid.Parse(id),
            Category = category
        };

        fixture.Guild.ReprimandHistory.Add(reprimand);
        return reprimand;
    }
}
