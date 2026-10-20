using System;
using System.Collections.Generic;
using Discord;
using HuTao.Services.Core.Preconditions;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core.Preconditions;

public class TeamAccessTests
{
    private const ulong Owner = 1;
    private const ulong Member = 2;

    private static ITeamMember TeamMember(ulong id, MembershipState state, TeamRole role)
    {
        var member = new Mock<ITeamMember>();
        member.SetupGet(m => m.User).Returns(Mock.Of<IUser>(u => u.Id == id));
        member.SetupGet(m => m.MembershipState).Returns(state);
        member.SetupGet(m => m.Role).Returns(role);
        return member.Object;
    }

    private static IApplication Application(params ITeamMember[] members)
    {
        var team = new Mock<ITeam>();
        team.SetupGet(t => t.OwnerUserId).Returns(Owner);
        team.SetupGet(t => t.TeamMembers).Returns(members);

        var application = new Mock<IApplication>();
        application.SetupGet(a => a.Owner).Returns(Mock.Of<IUser>(u => u.Id == 99));
        application.SetupGet(a => a.Team).Returns(team.Object);
        return application.Object;
    }

    [Theory]
    [InlineData(TeamRole.Admin)]
    [InlineData(TeamRole.Developer)]
    [InlineData(TeamRole.Owner)]
    public void AcceptedAdminOrDeveloper_IsTeamMember(TeamRole role)
        => Assert.True(TeamAccess.IsTeamMember(
            Application(TeamMember(Member, MembershipState.Accepted, role)), Member));

    [Fact]
    public void ReadOnlyMember_IsNotTeamMember()
        => Assert.False(TeamAccess.IsTeamMember(
            Application(TeamMember(Member, MembershipState.Accepted, TeamRole.ReadOnly)), Member));

    [Fact]
    public void MemberWhoHasNotAcceptedTheInvite_IsNotTeamMember()
        => Assert.False(TeamAccess.IsTeamMember(
            Application(TeamMember(Member, MembershipState.Invited, TeamRole.Admin)), Member));

    [Fact]
    public void TeamOwner_IsTeamMember()
        => Assert.True(TeamAccess.IsTeamMember(Application(), Owner));

    [Fact]
    public void ApplicationWithoutATeam_BelongsToItsOwner()
    {
        var application = new Mock<IApplication>();
        application.SetupGet(a => a.Owner).Returns(Mock.Of<IUser>(u => u.Id == Owner));
        application.SetupGet(a => a.Team).Returns((ITeam?) null!);

        Assert.True(TeamAccess.IsTeamMember(application.Object, Owner));
        Assert.False(TeamAccess.IsTeamMember(application.Object, Member));
    }
}
