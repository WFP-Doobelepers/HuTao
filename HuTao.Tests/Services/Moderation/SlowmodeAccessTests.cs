using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Moq;
using Xunit;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Tests.Services.Moderation;

// The slowmode commands let a member through with the Slowmode scope or with Manage Channels in the channel where the
// command runs, and they can name another channel. The member needs the scope, or Manage Channels in the channel that
// the command changes.
public class SlowmodeAccessTests
{
    private const ulong MemberId = 100;

    [Fact]
    public async Task ManageChannelsInTheChangedChannel_MayChangeIt()
    {
        var fixture = new AuthorizationFixture();
        var member = Member(fixture, manageChannels: true);

        Assert.Null(await RefuseAsync(fixture, member));
    }

    [Fact]
    public async Task ManageChannelsOnlyWhereTheCommandRuns_IsRefusedAnotherChannel()
    {
        var fixture = new AuthorizationFixture();
        var member = Member(fixture, manageChannels: false);

        Assert.Equal("You need the Slowmode scope, or the Manage Channels permission in <#903>.",
            await RefuseAsync(fixture, member));
    }

    [Fact]
    public async Task TheSlowmodeScope_MayChangeAnyChannel()
    {
        var fixture = new AuthorizationFixture();
        var member = Member(fixture, manageChannels: false);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Slowmode, member);

        Assert.Null(await RefuseAsync(fixture, member));
    }

    // The member's permissions in the channel that the command changes.
    private static IGuildUser Member(AuthorizationFixture fixture, bool manageChannels)
    {
        var member = fixture.User(MemberId);
        Mock.Get(member)
            .Setup(u => u.GetPermissions(It.IsAny<IGuildChannel>()))
            .Returns(new ChannelPermissions(viewChannel: true, manageChannel: manageChannels));
        return member;
    }

    private static Task<string?> RefuseAsync(AuthorizationFixture fixture, IGuildUser member)
        => fixture.Auth.RefuseSlowmodeAsync(
            new InteractionContext(fixture.Interaction(member)),
            Mock.Of<ITextChannel>(c => c.Id == 903 && c.Mention == "<#903>"));
}
