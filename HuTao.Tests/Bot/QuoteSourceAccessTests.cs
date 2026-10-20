using System.Threading.Tasks;
using Discord;
using HuTao.Bot.Modules;
using Moq;
using Xunit;

namespace HuTao.Tests.Bot;

/// <summary>
///     Expanding a quote, "Show all" and Context pull messages out of the source channel, so the member needs to be able
///     to read that channel. A thread is read through its parent channel, and a private thread only by its members.
/// </summary>
public class QuoteSourceAccessTests
{
    private const ulong ParentId = 7;
    private const ulong UserId = 3;

    private static Mock<IGuildUser> UserWith(ChannelPermissions permissions, IGuildChannel? parent = null)
    {
        var guild = new Mock<IGuild>();
        guild.Setup(g => g.GetChannelAsync(ParentId, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>())).ReturnsAsync(parent!);

        var user = new Mock<IGuildUser>();
        user.SetupGet(u => u.Id).Returns(UserId);
        user.SetupGet(u => u.Guild).Returns(guild.Object);
        user.Setup(u => u.GetPermissions(It.IsAny<IGuildChannel>())).Returns(permissions);
        return user;
    }

    private static IThreadChannel Thread(ThreadType type, bool member)
    {
        var thread = new Mock<IThreadChannel>();
        thread.SetupGet(t => t.CategoryId).Returns(ParentId);
        thread.SetupGet(t => t.Type).Returns(type);
        thread.Setup(t => t.GetUserAsync(UserId, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(member ? Mock.Of<IGuildUser>() : null!);
        return thread.Object;
    }

    private static readonly ChannelPermissions Read = ChannelPermissions.None.Modify(viewChannel: true, readMessageHistory: true);

    [Fact]
    public async Task MemberWhoCannotViewTheChannel_IsRefused()
    {
        var user = UserWith(ChannelPermissions.None.Modify(readMessageHistory: true));

        Assert.False(await InteractiveQuoteModule.CanReadSourceAsync(user.Object, Mock.Of<IGuildChannel>()));
    }

    [Fact]
    public async Task MemberWhoCannotReadHistory_IsRefused()
    {
        var user = UserWith(ChannelPermissions.None.Modify(viewChannel: true));

        Assert.False(await InteractiveQuoteModule.CanReadSourceAsync(user.Object, Mock.Of<IGuildChannel>()));
    }

    [Fact]
    public async Task MemberWhoCanReadTheChannel_IsAllowed()
    {
        var user = UserWith(Read);

        Assert.True(await InteractiveQuoteModule.CanReadSourceAsync(user.Object, Mock.Of<IGuildChannel>()));
    }

    [Fact]
    public async Task MissingChannelOrUser_IsRefused()
    {
        var user = UserWith(Read);

        Assert.False(await InteractiveQuoteModule.CanReadSourceAsync(user.Object, null));
        Assert.False(await InteractiveQuoteModule.CanReadSourceAsync(null, Mock.Of<IGuildChannel>()));
    }

    // A private thread, such as a thread ticket, is hidden from members of its parent channel who are not in it.
    [Fact]
    public async Task APrivateThread_IsRefused_ToAMemberWhoIsNotInIt()
    {
        var user = UserWith(Read, Mock.Of<IGuildChannel>());

        Assert.False(await InteractiveQuoteModule.CanReadSourceAsync(user.Object, Thread(ThreadType.PrivateThread, member: false)));
        Assert.True(await InteractiveQuoteModule.CanReadSourceAsync(user.Object, Thread(ThreadType.PrivateThread, member: true)));
    }

    [Fact]
    public async Task APrivateThread_IsAllowed_ToStaffWhoManageThreads()
    {
        var user = UserWith(Read.Modify(manageThreads: true), Mock.Of<IGuildChannel>());

        Assert.True(await InteractiveQuoteModule.CanReadSourceAsync(user.Object, Thread(ThreadType.PrivateThread, member: false)));
    }

    // A public thread is read with its parent channel's permissions.
    [Fact]
    public async Task APublicThread_FollowsItsParentChannel()
    {
        var reader = UserWith(Read, Mock.Of<IGuildChannel>());
        var outsider = UserWith(ChannelPermissions.None, Mock.Of<IGuildChannel>());

        Assert.True(await InteractiveQuoteModule.CanReadSourceAsync(reader.Object, Thread(ThreadType.PublicThread, member: false)));
        Assert.False(await InteractiveQuoteModule.CanReadSourceAsync(outsider.Object, Thread(ThreadType.PublicThread, member: false)));
    }
}
