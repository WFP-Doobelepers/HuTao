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

    private static IRole Role(string name, int position)
    {
        var role = new Mock<IRole>();
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
        user.SetupGet(u => u.GuildPermissions).Returns(permissions ?? GuildPermissions.None);
        return user.Object;
    }
}
