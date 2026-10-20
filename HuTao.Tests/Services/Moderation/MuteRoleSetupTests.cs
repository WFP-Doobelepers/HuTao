using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Moderation;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

/// <summary>
///     Setting up a mute role writes to every channel as the bot, so it must change only the four mute permissions and
///     must not take a role the invoking member could not give themselves.
/// </summary>
public class MuteRoleSetupTests
{
    private const ulong GuildId = 77;

    [Fact]
    public async Task AddMuteOverwrites_KeepsTheRolesOtherOverwriteBits()
    {
        var role = Role("Muted", 2);
        var existing = new OverwritePermissions(
            viewChannel: PermValue.Deny, readMessageHistory: PermValue.Allow, sendMessages: PermValue.Allow);

        var channel = new Mock<IGuildChannel>();
        channel.Setup(c => c.GetPermissionOverwrite(role)).Returns(existing);
        OverwritePermissions? written = null;
        channel
            .Setup(c => c.AddPermissionOverwriteAsync(role, It.IsAny<OverwritePermissions>(), It.IsAny<RequestOptions>()))
            .Callback((IRole _, OverwritePermissions o, RequestOptions _) => written = o)
            .Returns(Task.CompletedTask);

        await ModerationService.AddMuteOverwritesAsync(Guild([channel.Object]), role);

        Assert.NotNull(written);
        Assert.Equal(PermValue.Deny, written.Value.ViewChannel);
        Assert.Equal(PermValue.Allow, written.Value.ReadMessageHistory);
        Assert.Equal(PermValue.Deny, written.Value.SendMessages);
        Assert.Equal(PermValue.Deny, written.Value.AddReactions);
        Assert.Equal(PermValue.Deny, written.Value.Speak);
        Assert.Equal(PermValue.Deny, written.Value.Stream);
    }

    [Fact]
    public async Task AddMuteOverwrites_WithNoOverwrite_StartsFromAnEmptyOne()
    {
        var role = Role("Muted", 2);
        var channel = new Mock<IGuildChannel>();
        channel.Setup(c => c.GetPermissionOverwrite(role)).Returns((OverwritePermissions?) null);
        OverwritePermissions? written = null;
        channel
            .Setup(c => c.AddPermissionOverwriteAsync(role, It.IsAny<OverwritePermissions>(), It.IsAny<RequestOptions>()))
            .Callback((IRole _, OverwritePermissions o, RequestOptions _) => written = o)
            .Returns(Task.CompletedTask);

        await ModerationService.AddMuteOverwritesAsync(Guild([channel.Object]), role);

        Assert.Equal(PermValue.Deny, written!.Value.SendMessages);
        Assert.Equal(PermValue.Inherit, written.Value.ViewChannel);
    }

    [Fact]
    public async Task ResolveMuteRole_ExistingRoleFoundByName_AtTheInvokersLevel_Throws()
    {
        var muted = Role("Muted", 10, id: 5);
        var guild = Guild([], [muted]);

        var thrown = await Assert.ThrowsAsync<UserFacingException>(() =>
            ModerationService.ResolveMuteRoleAsync(guild, null, [], "Muted", Invoker()));

        Assert.Equal("Muted is higher or equal than your roles.", thrown.Message);
    }

    [Fact]
    public async Task ResolveMuteRole_ConfiguredEveryone_Throws()
    {
        var everyone = Role("@everyone", 0, id: GuildId);
        var guild = Guild([], [everyone]);

        await Assert.ThrowsAsync<UserFacingException>(() =>
            ModerationService.ResolveMuteRoleAsync(guild, null, [GuildId], "Muted", Invoker()));
    }

    [Fact]
    public async Task ResolveMuteRole_ExistingRoleBelowTheInvoker_IsReturned()
    {
        var muted = Role("Muted", 3, id: 5);
        var guild = Guild([], [muted]);

        var role = await ModerationService.ResolveMuteRoleAsync(guild, null, [], "Muted", Invoker());

        Assert.Same(muted, role);
    }

    [Fact]
    public async Task ResolveMuteRole_NothingFound_CreatesTheRole()
    {
        var created = Role("Muted", 1, id: 9);
        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Roles).Returns(new List<IRole>());
        guild
            .Setup(g => g.CreateRoleAsync("Muted", It.IsAny<GuildPermissions?>(), It.IsAny<Color?>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(created);

        var role = await ModerationService.ResolveMuteRoleAsync(guild.Object, null, [], "Muted", Invoker());

        Assert.Same(created, role);
    }

    private static IGuildUser Invoker()
    {
        var user = new Mock<IGuildUser>();
        user.SetupGet(u => u.Hierarchy).Returns(10);
        user.SetupGet(u => u.GuildId).Returns(GuildId);
        return user.Object;
    }

    private static IGuild Guild(IEnumerable<IGuildChannel> channels, IEnumerable<IRole>? roles = null)
    {
        var bot = new Mock<IGuildUser>();
        bot.SetupGet(u => u.RoleIds).Returns([]);

        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Id).Returns(GuildId);
        guild.SetupGet(g => g.Roles).Returns(roles?.ToList() ?? []);
        guild
            .Setup(g => g.GetChannelsAsync(It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(channels.ToList());
        guild
            .Setup(g => g.GetCurrentUserAsync(It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(bot.Object);
        return guild.Object;
    }

    private static IRole Role(string name, int position, ulong id = 1)
    {
        var role = new Mock<IRole>();
        role.SetupGet(r => r.Id).Returns(id);
        role.SetupGet(r => r.Position).Returns(position);
        role.Setup(r => r.ToString()).Returns(name);
        role.SetupGet(r => r.Name).Returns(name);
        return role.Object;
    }
}
