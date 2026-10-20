using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Services.Core.Preconditions.Commands;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core.Preconditions;

public class RequireHigherRoleAttributeTests
{
    private const int ModeratorHierarchy = 10;
    private const int BotHierarchy = 20;

    [Fact]
    public async Task ParamsArray_WithMemberAtModeratorHierarchy_Fails()
    {
        IUser[] users = [GuildUser(ModeratorHierarchy - 5), GuildUser(ModeratorHierarchy)];

        var result = await CheckAsync(users);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("You cannot ban", result.ErrorReason);
    }

    [Fact]
    public async Task UserList_WithMemberAboveModerator_Fails()
    {
        var users = new List<IUser> { GuildUser(ModeratorHierarchy + 5) };

        var result = await CheckAsync(users);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("You cannot ban", result.ErrorReason);
    }

    [Fact]
    public async Task UserList_WithMemberAboveBot_Fails()
    {
        var users = new List<IUser> { GuildUser(BotHierarchy + 5) };

        var result = await CheckAsync(users, moderator: BotHierarchy + 10);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("The bot's role is lower", result.ErrorReason);
    }

    [Fact]
    public async Task UserList_WithLowerMembersAndNonMember_Succeeds()
    {
        var users = new List<IUser> { GuildUser(ModeratorHierarchy - 5), Mock.Of<IUser>() };

        var result = await CheckAsync(users);

        Assert.True(result.IsSuccess, result.ErrorReason);
    }

    [Fact]
    public async Task GuildUserList_WithMemberAboveModerator_Fails()
    {
        var users = new List<IGuildUser> { GuildUser(ModeratorHierarchy + 5) };

        var result = await CheckAsync(users);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("You cannot ban", result.ErrorReason);
    }

    private static async Task<PreconditionResult> CheckAsync(
        object value, int moderator = ModeratorHierarchy, int bot = BotHierarchy)
    {
        var guild = new Mock<IGuild>();
        guild
            .Setup(g => g.GetCurrentUserAsync(It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(GuildUser(bot));

        var context = new Mock<ICommandContext>();
        context.SetupGet(c => c.User).Returns(GuildUser(moderator));
        context.SetupGet(c => c.Guild).Returns(guild.Object);

        var parameter = await BanParameterAsync();
        return await new RequireHigherRoleAttribute()
            .CheckPermissionsAsync(context.Object, parameter, value, Mock.Of<IServiceProvider>());
    }

    /// <summary>
    ///     The error message names the command, so the parameter needs a real command behind it.
    /// </summary>
    private static async Task<ParameterInfo> BanParameterAsync()
    {
        var module = await new CommandService().CreateModuleAsync(string.Empty, m => m
            .AddCommand("ban", (_, _, _, _) => Task.CompletedTask, c => c
                .AddParameter<string>("users", _ => { })));

        return module.Commands[0].Parameters[0];
    }

    private static IGuildUser GuildUser(int hierarchy)
    {
        var user = new Mock<IGuildUser>();
        user.SetupGet(u => u.Hierarchy).Returns(hierarchy);
        return user.Object;
    }
}
