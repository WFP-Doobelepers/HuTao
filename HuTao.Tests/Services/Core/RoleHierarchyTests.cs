using System.Collections.Generic;
using Discord;
using HuTao.Services.Core;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core;

public class RoleHierarchyTests
{
    private const int InvokerHierarchy = 10;
    private const ulong GuildOwnerId = 1;
    private const ulong MemberId = 2;
    private const ulong GuildId = 77;
    private const ulong BotRoleId = 600;

    [Fact]
    public void CheckRoles_RoleBelowInvoker_Passes()
    {
        var error = RoleHierarchy.CheckRoles(Member(), [Role("Helper", InvokerHierarchy - 1)]);

        Assert.Null(error);
    }

    [Fact]
    public void CheckRoles_RoleAtInvoker_Fails()
    {
        var error = RoleHierarchy.CheckRoles(Member(), [Role("Moderator", InvokerHierarchy)]);

        Assert.Equal("Moderator is higher or equal than your roles.", error);
    }

    [Fact]
    public void CheckRoles_RoleAboveInvoker_Fails()
    {
        var error = RoleHierarchy.CheckRoles(Member(), [Role("Admin", InvokerHierarchy + 5)]);

        Assert.Equal("Admin is higher or equal than your roles.", error);
    }

    [Fact]
    public void CheckRoles_ReportsFirstOffendingRole_AcrossAllGroups()
    {
        var error = RoleHierarchy.CheckRoles(
            Member(),
            [Role("Helper", InvokerHierarchy - 1)],
            null,
            [Role("Admin", InvokerHierarchy + 5), Role("Owner", InvokerHierarchy + 9)]);

        Assert.Equal("Admin is higher or equal than your roles.", error);
    }

    [Fact]
    public void CheckRoles_NoRoles_Passes()
    {
        Assert.Null(RoleHierarchy.CheckRoles(Member(), null, null, null));
        Assert.Null(RoleHierarchy.CheckRoles(Member(), new List<IRole>()));
    }

    [Fact]
    public void CheckRoles_GuildOwner_PassesForAnyRole()
    {
        var owner = Member(hierarchy: int.MaxValue, id: GuildOwnerId);

        var error = RoleHierarchy.CheckRoles(owner, [Role("Admin", 250)]);

        Assert.Null(error);
    }

    [Fact]
    public void CheckRole_RoleAtInvoker_Fails()
    {
        var error = RoleHierarchy.CheckRole(Member(), Role("Moderator", InvokerHierarchy));

        Assert.Equal("Moderator is higher or equal than your roles.", error);
    }

    [Fact]
    public void CheckRole_RoleBelowInvoker_Passes()
    {
        Assert.Null(RoleHierarchy.CheckRole(Member(), Role("Helper", InvokerHierarchy - 1)));
    }

    [Fact]
    public void CheckPermissions_PermissionInvokerLacks_Fails()
    {
        var invoker = Member(permissions: new GuildPermissions(manageRoles: true));

        var error = RoleHierarchy.CheckPermissions(invoker, new GuildPermissions(manageRoles: true, banMembers: true));

        Assert.NotNull(error);
        Assert.Contains("Ban members", error);
        Assert.DoesNotContain("Manage roles", error);
    }

    [Fact]
    public void CheckPermissions_SubsetOfInvokerPermissions_Passes()
    {
        var invoker = Member(permissions: new GuildPermissions(manageRoles: true, banMembers: true));

        Assert.Null(RoleHierarchy.CheckPermissions(invoker, new GuildPermissions(banMembers: true)));
    }

    [Fact]
    public void CheckPermissions_AdministratorRequestedByNonAdministrator_Fails()
    {
        var invoker = Member(permissions: new GuildPermissions(manageRoles: true));

        Assert.NotNull(RoleHierarchy.CheckPermissions(invoker, new GuildPermissions(administrator: true)));
    }

    [Fact]
    public void CheckPermissions_InvokerWithAdministrator_Passes()
    {
        var invoker = Member(permissions: new GuildPermissions(administrator: true));

        Assert.Null(RoleHierarchy.CheckPermissions(invoker, GuildPermissions.All));
    }

    [Fact]
    public void CheckPermissions_GuildOwner_Passes()
    {
        var owner = Member(id: GuildOwnerId, permissions: GuildPermissions.None);

        Assert.Null(RoleHierarchy.CheckPermissions(owner, new GuildPermissions(administrator: true)));
    }

    [Fact]
    public void CheckMuteRole_Everyone_Fails_EvenThoughItsPositionIsBelowEveryRole()
    {
        var everyone = Role("@everyone", 0, id: GuildId);

        var error = RoleHierarchy.CheckMuteRole(Member(), everyone, Bot());

        Assert.Equal("@everyone cannot be used as a mute role.", error);
    }

    [Fact]
    public void CheckMuteRole_ManagedRole_Fails()
    {
        var error = RoleHierarchy.CheckMuteRole(Member(), Role("Booster", 1, managed: true), Bot());

        Assert.Equal("Booster is managed by an integration and cannot be used as a mute role.", error);
    }

    [Fact]
    public void CheckMuteRole_RoleTheBotHolds_Fails()
    {
        var error = RoleHierarchy.CheckMuteRole(Member(), Role("Hu Tao", 3, id: BotRoleId), Bot());

        Assert.Equal("Hu Tao is one of the bot's roles and cannot be used as a mute role.", error);
    }

    [Fact]
    public void CheckMuteRole_RoleAtInvoker_Fails()
    {
        var error = RoleHierarchy.CheckMuteRole(Member(), Role("Moderator", InvokerHierarchy), Bot());

        Assert.Equal("Moderator is higher or equal than your roles.", error);
    }

    [Fact]
    public void CheckMuteRole_PlainRoleBelowInvoker_Passes()
    {
        Assert.Null(RoleHierarchy.CheckMuteRole(Member(), Role("Muted", 2), Bot()));
    }

    // The moderation panels act on the member a mute or timeout belongs to, so they make the rank check that a command
    // parameter gets from RequireHigherRole.
    [Fact]
    public void CheckTarget_AtOrAboveTheModerator_Fails()
    {
        Assert.NotNull(RoleHierarchy.CheckTarget(Member(), Member(InvokerHierarchy, id: 77), Member(100, id: 2)));
        Assert.NotNull(RoleHierarchy.CheckTarget(Member(), Member(InvokerHierarchy + 5, id: 77), Member(100, id: 2)));
    }

    [Fact]
    public void CheckTarget_AtOrAboveTheBot_Fails()
    {
        Assert.NotNull(RoleHierarchy.CheckTarget(Member(100), Member(50, id: 77), Member(50, id: 2)));
    }

    [Fact]
    public void CheckTarget_BelowBoth_Passes_AndAUserWhoLeftHasNoRank()
    {
        Assert.Null(RoleHierarchy.CheckTarget(Member(), Member(InvokerHierarchy - 5, id: 77), Member(100, id: 2)));
        Assert.Null(RoleHierarchy.CheckTarget(Member(), null, Member(100, id: 2)));
    }

    private static IGuildUser Bot()
    {
        var bot = new Mock<IGuildUser>();
        bot.SetupGet(u => u.RoleIds).Returns([BotRoleId]);
        return bot.Object;
    }

    private static IRole Role(string name, int position, ulong id = 500, bool managed = false)
    {
        var role = new Mock<IRole>();
        role.SetupGet(r => r.Id).Returns(id);
        role.SetupGet(r => r.IsManaged).Returns(managed);
        role.SetupGet(r => r.Position).Returns(position);
        role.Setup(r => r.ToString()).Returns(name);
        return role.Object;
    }

    private static IGuildUser Member(
        int hierarchy = InvokerHierarchy, ulong id = MemberId, GuildPermissions? permissions = null)
    {
        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.OwnerId).Returns(GuildOwnerId);

        var user = new Mock<IGuildUser>();
        user.SetupGet(u => u.Id).Returns(id);
        user.SetupGet(u => u.Hierarchy).Returns(hierarchy);
        user.SetupGet(u => u.Guild).Returns(guild.Object);
        user.SetupGet(u => u.GuildId).Returns(GuildId);
        user.SetupGet(u => u.GuildPermissions).Returns(permissions ?? GuildPermissions.None);
        return user.Object;
    }
}
