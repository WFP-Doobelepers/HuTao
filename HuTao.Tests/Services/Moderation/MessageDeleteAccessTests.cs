using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Moq;
using Xunit;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Tests.Services.Moderation;

// The Delete button of an old quote names a channel and a message in its custom ID, and the bot deletes the message
// with its own permissions. The button asks for what the purge commands ask for: the Purge scope, and Discord's Manage
// Messages permission in the message's channel.
public class MessageDeleteAccessTests
{
    private const ulong MemberId = 100;

    [Fact]
    public async Task ThePurgeScopeAndManageMessagesInTheChannel_MayDelete()
    {
        var fixture = new AuthorizationFixture();
        var member = Member(fixture, manageMessages: true);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Purge, member);

        Assert.True(await MayDeleteAsync(fixture, member));
    }

    [Fact]
    public async Task ThePurgeScope_WithoutManageMessagesInTheChannel_IsRefused()
    {
        var fixture = new AuthorizationFixture();
        var member = Member(fixture, manageMessages: false);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Purge, member);

        Assert.False(await MayDeleteAsync(fixture, member));
    }

    [Fact]
    public async Task ManageMessagesInTheChannel_WithoutThePurgeScope_IsRefused()
    {
        var fixture = new AuthorizationFixture();
        var member = Member(fixture, manageMessages: true);

        Assert.False(await MayDeleteAsync(fixture, member));
    }

    private static IGuildUser Member(AuthorizationFixture fixture, bool manageMessages)
    {
        var member = fixture.User(MemberId);
        Mock.Get(member)
            .Setup(u => u.GetPermissions(It.IsAny<IGuildChannel>()))
            .Returns(new ChannelPermissions(viewChannel: true, manageMessages: manageMessages));
        return member;
    }

    private static Task<bool> MayDeleteAsync(AuthorizationFixture fixture, IGuildUser member)
        => fixture.Auth.MayDeleteAsync(
            new InteractionContext(fixture.Interaction(member)), member, Mock.Of<ITextChannel>(c => c.Id == 903));
}
