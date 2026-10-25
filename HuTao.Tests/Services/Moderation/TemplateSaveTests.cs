using System.Threading.Tasks;
using Discord;
using HuTao.Bot.Modules.Moderation;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Moq;
using Xunit;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Tests.Services.Moderation;

// Anyone with a template's scope can apply it with the bot's authority, and its scope can be lower than its action, so a
// member may only save a template for an action they may take themselves.
public class TemplateSaveTests
{
    private const ulong MemberId = 100;

    [Fact]
    public async Task AMemberWithOnlyConfiguration_IsRefusedABanTemplate()
    {
        var (fixture, member) = Member(AuthorizationScope.Configuration);

        var refusal = await RefuseAsync(fixture, member, new BanAction(0, null));

        Assert.Equal(
            "You can only save a template for an action you may take yourself, and you do not have the Ban scope.",
            refusal);
    }

    [Fact]
    public async Task AMemberWithBan_MaySaveABanTemplate()
    {
        var (fixture, member) = Member(AuthorizationScope.Configuration | AuthorizationScope.Ban);

        Assert.Null(await RefuseAsync(fixture, member, new BanAction(0, null)));
    }

    [Fact]
    public async Task AMemberWithBanInTheTemplatesCategory_MaySaveABanTemplateThere()
    {
        var (fixture, member) = Member(AuthorizationScope.Configuration);
        var raids = fixture.AddCategory("Raids");
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Ban, member);

        Assert.Null(await RefuseAsync(fixture, member, new BanAction(0, null), raids));
    }

    [Fact]
    public async Task AMemberWithBanOnlyInAnotherCategory_IsRefused()
    {
        var (fixture, member) = Member(AuthorizationScope.Configuration);
        var raids = fixture.AddCategory("Raids");
        var spam = fixture.AddCategory("Spam");
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Ban, member);

        Assert.NotNull(await RefuseAsync(fixture, member, new BanAction(0, null), spam));
    }

    [Fact]
    public async Task ANoticeTemplate_NeedsTheWarningScope()
    {
        var (fixture, member) = Member(AuthorizationScope.Configuration | AuthorizationScope.Note);

        Assert.Contains("Warning", await RefuseAsync(fixture, member, new NoticeAction()));
    }

    [Fact]
    public async Task ARoleTemplate_NeedsTheManageRolesScope()
    {
        var (fixture, member) = Member(AuthorizationScope.Configuration);

        Assert.Contains("ManageRoles", await RefuseAsync(fixture, member, RoleAction()));
    }

    [Fact]
    public async Task ARoleTemplate_IsAllowedWithDiscordsManageRolesPermission()
    {
        var (fixture, member) = Member(AuthorizationScope.Configuration);
        Mock.Get(member).SetupGet(u => u.GuildPermissions).Returns(new GuildPermissions(manageRoles: true));

        Assert.Null(await RefuseAsync(fixture, member, RoleAction()));
    }

    [Fact]
    public async Task TheServerOwner_MaySaveAnyTemplate()
    {
        var (fixture, member) = Member(AuthorizationScope.None);
        Mock.Get(fixture.DiscordGuild).SetupGet(g => g.OwnerId).Returns(MemberId);

        Assert.Null(await RefuseAsync(fixture, member, new BanAction(0, null)));
    }

    private static RoleAction RoleAction() => new(null, new ModerationTemplatesModule.RoleTemplateOptions());

    private static (AuthorizationFixture Fixture, IGuildUser Member) Member(AuthorizationScope scopes)
    {
        var fixture = new AuthorizationFixture();
        var member = fixture.User(MemberId);
        if (scopes != AuthorizationScope.None)
            AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, scopes, member);

        return (fixture, member);
    }

    private static Task<string?> RefuseAsync(
        AuthorizationFixture fixture, IGuildUser member, ReprimandAction action, ModerationCategory? category = null)
        => fixture.Auth.RefuseSaveAsync(new CommandContext(fixture.Command(member)), member, action, category);
}
