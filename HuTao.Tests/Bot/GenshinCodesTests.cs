using System;
using Discord;
using HuTao.Bot.Modules;
using Moq;
using Xunit;

namespace HuTao.Tests.Bot;

public class GenshinCodesTests
{
    private static IGuildUser UserWith(ulong id, ChannelPermissions permissions, params ulong[] roles)
    {
        var user = new Mock<IGuildUser>();
        user.SetupGet(u => u.Id).Returns(id);
        user.SetupGet(u => u.RoleIds).Returns(roles);
        user.Setup(u => u.GetPermissions(It.IsAny<IGuildChannel>())).Returns(permissions);
        return user.Object;
    }

    [Theory]
    [InlineData("LunaI0910", true)]
    [InlineData("abcd", true)]
    [InlineData("abc", false)]
    [InlineData("a b c d", false)]
    [InlineData("code)[x](https://evil.example", false)]
    [InlineData("code&x=1", false)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    [InlineData("", false)]
    public void CodeShape_IsChecked(string code, bool valid)
        => Assert.Equal(valid, GenshinModule.IsValidCode(code));

    [Fact]
    public void RedeemUrl_EscapesTheCode()
        => Assert.Equal("https://genshin.hoyoverse.com/en/gift?code=a%26b%3D1", GenshinModule.RedeemUrl("a&b=1"));

    [Fact]
    public void PostingToAChannel_NeedsManageWebhooksOrTheAllowList()
    {
        var channel = Mock.Of<IGuildChannel>();

        var plain = UserWith(5001, ChannelPermissions.None.Modify(sendMessages: true));
        var manager = UserWith(5002, ChannelPermissions.None.Modify(sendMessages: true, manageWebhooks: true));
        var allowed = UserWith(5003, ChannelPermissions.None.Modify(sendMessages: true));
        GenshinModule.AllowedUsers.Add(5003);

        Assert.False(GenshinModule.CanPostCodes(plain, channel));
        Assert.True(GenshinModule.CanPostCodes(manager, channel));
        Assert.True(GenshinModule.CanPostCodes(allowed, channel));
    }

    [Fact]
    public void ImportingFromAMessage_NeedsViewAndHistoryOnItsChannel()
    {
        var channel = Mock.Of<IGuildChannel>();

        Assert.False(GenshinModule.CanReadChannel(
            UserWith(1, ChannelPermissions.None.Modify(readMessageHistory: true)), channel));
        Assert.False(GenshinModule.CanReadChannel(
            UserWith(1, ChannelPermissions.None.Modify(viewChannel: true)), channel));
        Assert.True(GenshinModule.CanReadChannel(
            UserWith(1, ChannelPermissions.None.Modify(viewChannel: true, readMessageHistory: true)), channel));
        Assert.False(GenshinModule.CanReadChannel(null, channel));
        Assert.False(GenshinModule.CanReadChannel(UserWith(1, ChannelPermissions.None.Modify(viewChannel: true, readMessageHistory: true)), null));
    }

    [Fact]
    public void PostingTwice_InsideTheCooldownIsRefused()
    {
        var now = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

        Assert.True(GenshinModule.TryStartCodesCooldown(7001, now, out _));
        Assert.False(GenshinModule.TryStartCodesCooldown(7001, now.AddSeconds(4), out var remaining));
        Assert.Equal(TimeSpan.FromSeconds(6), remaining);
        Assert.True(GenshinModule.TryStartCodesCooldown(7002, now.AddSeconds(4), out _));
        Assert.True(GenshinModule.TryStartCodesCooldown(7001, now.AddSeconds(11), out _));
    }
}
