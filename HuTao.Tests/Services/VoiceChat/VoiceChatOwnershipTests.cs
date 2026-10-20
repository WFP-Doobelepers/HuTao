using System;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.VoiceChat;
using HuTao.Services.VoiceChat;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.VoiceChat;

public class VoiceChatOwnershipTests
{
    private const ulong TextChannelId = 10;
    private const ulong OldOwner = 100;
    private const ulong NewOwner = 200;

    [Fact]
    public async Task GetOwnerId_ReadsTheCurrentOwner_AfterATransfer()
    {
        var options = new DbContextOptionsBuilder<HuTaoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new HuTaoContext(options);
        db.Add(new VoiceChatLink { TextChannelId = TextChannelId, VoiceChannelId = 11, GuildId = 1, UserId = OldOwner });
        await db.SaveChangesAsync();

        Assert.Equal(OldOwner, await VoiceChatOwnership.GetOwnerIdAsync(db, TextChannelId));

        // A transfer made elsewhere, while an old panel stays open.
        await using (var other = new HuTaoContext(options))
        {
            var link = await other.Set<VoiceChatLink>().SingleAsync();
            link.UserId = NewOwner;
            await other.SaveChangesAsync();
        }

        Assert.Equal(NewOwner, await VoiceChatOwnership.GetOwnerIdAsync(db, TextChannelId));
    }

    [Fact]
    public async Task GetOwnerId_WithNoLink_IsNull()
    {
        await using var db = new HuTaoContext(new DbContextOptionsBuilder<HuTaoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        Assert.Null(await VoiceChatOwnership.GetOwnerIdAsync(db, TextChannelId));
    }

    [Fact]
    public void EveryoneOverwrite_ChangesOnlyTheRequestedBit()
    {
        var everyone = Mock.Of<IRole>();
        var channel = new Mock<IGuildChannel>();
        channel.Setup(c => c.GetPermissionOverwrite(everyone)).Returns(new OverwritePermissions(
            sendMessages: PermValue.Deny, connect: PermValue.Allow));

        var hidden = VoiceChatOwnership.EveryoneOverwrite(channel.Object, everyone, viewChannel: PermValue.Deny);

        Assert.Equal(PermValue.Deny, hidden.ViewChannel);
        Assert.Equal(PermValue.Deny, hidden.SendMessages);
        Assert.Equal(PermValue.Allow, hidden.Connect);
    }

    [Fact]
    public void EveryoneOverwrite_WithNoExistingOverwrite_SetsOnlyTheRequestedBit()
    {
        var everyone = Mock.Of<IRole>();
        var channel = Mock.Of<IGuildChannel>();

        var locked = VoiceChatOwnership.EveryoneOverwrite(channel, everyone, connect: PermValue.Deny);

        Assert.Equal(PermValue.Deny, locked.Connect);
        Assert.Equal(PermValue.Inherit, locked.ViewChannel);
    }

    [Fact]
    public async Task Transfer_RemovesTheOldOwnersOverwrite_NotTheInvokers()
    {
        var channel = new Mock<IGuildChannel>();
        var oldOwner = Mock.Of<IGuildUser>(u => u.Id == OldOwner);
        var newOwner = Mock.Of<IGuildUser>(u => u.Id == NewOwner);

        await VoiceChatOwnership.TransferOverwritesAsync(channel.Object, oldOwner, newOwner);

        channel.Verify(c => c.RemovePermissionOverwriteAsync(
            It.Is<IUser>(u => u.Id == OldOwner), It.IsAny<RequestOptions>()), Times.Once);
        channel.Verify(c => c.RemovePermissionOverwriteAsync(
            It.Is<IUser>(u => u.Id != OldOwner), It.IsAny<RequestOptions>()), Times.Never);
        channel.Verify(c => c.AddPermissionOverwriteAsync(
            It.Is<IUser>(u => u.Id == NewOwner), It.Is<OverwritePermissions>(o => o.ManageChannel == PermValue.Allow),
            It.IsAny<RequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task Transfer_WithTheOldOwnerGone_StillGivesTheNewOwnerTheirOverwrite()
    {
        var channel = new Mock<IGuildChannel>();
        var newOwner = Mock.Of<IGuildUser>(u => u.Id == NewOwner);

        await VoiceChatOwnership.TransferOverwritesAsync(channel.Object, null, newOwner);

        channel.Verify(c => c.RemovePermissionOverwriteAsync(It.IsAny<IUser>(), It.IsAny<RequestOptions>()), Times.Never);
        channel.Verify(c => c.AddPermissionOverwriteAsync(
            It.IsAny<IUser>(), It.IsAny<OverwritePermissions>(), It.IsAny<RequestOptions>()), Times.Once);
    }
}
