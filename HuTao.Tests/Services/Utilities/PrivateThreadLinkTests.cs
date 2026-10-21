using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Services.Quote;
using HuTao.Services.Utilities;
using Moq;
using Xunit;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Tests.Services.Utilities;

/// <summary>
///     Discord.Net resolves a thread's permissions from its parent channel, so everyone who can read the parent channel
///     would read a private thread through a message link. The quote of a link and the commands that take a link read
///     only what the member could open in Discord.
/// </summary>
public class PrivateThreadLinkTests
{
    private const ulong GuildId = 1;
    private const ulong ThreadId = 20;
    private const ulong MessageId = 30;
    private const ulong MemberId = 100;

    private static readonly JumpMessage Link = new(GuildId, ThreadId, MessageId, false);

    [Fact]
    public async Task PrivateThread_IsHiddenFromAMemberWhoIsNotInIt()
        => Assert.Null(await Link.GetMessageAsync(Context(ThreadType.PrivateThread, inThread: false)));

    [Fact]
    public async Task PrivateThread_IsReadByItsMembers()
        => Assert.NotNull(await Link.GetMessageAsync(Context(ThreadType.PrivateThread, inThread: true)));

    [Fact]
    public async Task PrivateThread_IsReadByStaffWhoManageThreads()
        => Assert.NotNull(await Link.GetMessageAsync(
            Context(ThreadType.PrivateThread, inThread: false, manageThreads: true)));

    [Fact]
    public async Task PublicThread_IsReadByEveryoneWhoReadsItsChannel()
        => Assert.NotNull(await Link.GetMessageAsync(Context(ThreadType.PublicThread, inThread: false)));

    private static CommandContext Context(ThreadType type, bool inThread, bool manageThreads = false)
    {
        var thread = new Mock<IThreadChannel>();
        thread.SetupGet(t => t.Id).Returns(ThreadId);
        thread.SetupGet(t => t.Type).Returns(type);
        thread.As<IThreadMemberList>()
            .Setup(t => t.GetMemberIdsAsync())
            .ReturnsAsync(inThread ? new[] { MemberId } : Array.Empty<ulong>());
        thread.Setup(t => t.GetMessageAsync(MessageId, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(Mock.Of<IMessage>());

        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Id).Returns(GuildId);
        guild.Setup(g => g.GetTextChannelAsync(ThreadId, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(thread.Object);

        // The parent channel's permissions, as Discord.Net gives them for the thread.
        var member = new Mock<IGuildUser>();
        member.SetupGet(u => u.Id).Returns(MemberId);
        member.Setup(u => u.GetPermissions(It.IsAny<IGuildChannel>()))
            .Returns(new ChannelPermissions(viewChannel: true, readMessageHistory: true, manageThreads: manageThreads));

        var command = new Mock<ICommandContext>();
        command.SetupGet(c => c.Guild).Returns(guild.Object);
        command.SetupGet(c => c.User).Returns(member.Object);
        command.SetupGet(c => c.Channel).Returns(Mock.Of<ITextChannel>());

        return new CommandContext(command.Object);
    }
}
