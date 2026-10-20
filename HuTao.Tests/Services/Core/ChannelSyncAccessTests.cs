using Discord;
using HuTao.Services.Core;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core;

/// <summary>
///     Syncing a channel to its category replaces the channel's own overwrites, which can drop a deny that keeps a private
///     channel private, so a member may only sync channels they can see.
/// </summary>
public class ChannelSyncAccessTests
{
    [Fact]
    public void ChannelTheMemberCannotSee_Fails()
    {
        var error = ChannelSyncAccess.Check(Member(canView: false), Channel());

        Assert.NotNull(error);
    }

    [Fact]
    public void ChannelTheMemberCanSee_Passes()
    {
        Assert.Null(ChannelSyncAccess.Check(Member(canView: true), Channel()));
    }

    [Fact]
    public void Administrator_PassesWithoutSeeingTheChannel()
    {
        var admin = Member(canView: false, administrator: true);

        Assert.Null(ChannelSyncAccess.Check(admin, Channel()));
    }

    [Fact]
    public void GuildOwner_PassesWithoutSeeingTheChannel()
    {
        var owner = Member(canView: false, id: 1);

        Assert.Null(ChannelSyncAccess.Check(owner, Channel()));
    }

    private static IGuildChannel Channel() => Mock.Of<IGuildChannel>();

    private static IGuildUser Member(bool canView, bool administrator = false, ulong id = 2)
    {
        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.OwnerId).Returns(1);

        var user = new Mock<IGuildUser>();
        user.SetupGet(u => u.Id).Returns(id);
        user.SetupGet(u => u.Guild).Returns(guild.Object);
        user.SetupGet(u => u.GuildPermissions).Returns(new GuildPermissions(administrator: administrator));
        user.Setup(u => u.GetPermissions(It.IsAny<IGuildChannel>()))
            .Returns(new ChannelPermissions(viewChannel: canView));
        return user.Object;
    }
}
