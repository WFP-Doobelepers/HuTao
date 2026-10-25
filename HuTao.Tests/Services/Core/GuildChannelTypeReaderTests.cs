using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.TypeReaders.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core;

// A button's custom ID can carry a channel ID, and a member can send one with another server's channel. The reader
// finds the channel only in the server where the interaction happened.
public class GuildChannelTypeReaderTests
{
    private const ulong HereId = 5;
    private const ulong ElsewhereId = 6;

    [Fact]
    public async Task AChannelOfThisServer_IsRead()
    {
        var here = Mock.Of<ITextChannel>(c => c.Id == HereId);

        var result = await new GuildChannelTypeReader<ITextChannel>().ReadAsync(Context(here), $"{HereId}", null!);

        Assert.True(result.IsSuccess);
        Assert.Same(here, result.Value);
    }

    [Theory]
    [InlineData("6")]
    [InlineData("general")]
    public async Task AChannelOfAnotherServer_OrNoId_IsRefused(string option)
    {
        var here = Mock.Of<ITextChannel>(c => c.Id == HereId);

        var result = await new GuildChannelTypeReader<ITextChannel>().ReadAsync(Context(here), option, null!);

        Assert.False(result.IsSuccess);
    }

    // Discord.Net's own reader finds a channel in any server the bot is in, so the bot registers this reader for every
    // channel in a custom ID.
    [Fact]
    public async Task TheBot_ReadsChannelsInCustomIdsWithThisReader()
    {
        var discord = new DiscordSocketClient();
        var commands = new InteractionService(discord);
        var services = new ServiceCollection().BuildServiceProvider();
        var handling = new InteractionHandlingService(discord, commands, new InteractiveService(discord),
            NullLogger<InteractionHandlingService>.Instance, services.GetRequiredService<IServiceScopeFactory>());

        await handling.InitializeAsync();
        await commands.AddModuleAsync<ChannelButtonModule>(services);

        var channel = commands.ComponentCommands.Single(c => c.Name == "test-channel:*").Parameters.Single();
        Assert.IsType<GuildChannelTypeReader<ITextChannel>>(channel.TypeReader);
    }

    public class ChannelButtonModule : InteractionModuleBase<IInteractionContext>
    {
        [ComponentInteraction("test-channel:*")]
        public Task ChannelAsync(ITextChannel channel) => Task.CompletedTask;
    }

    private static IInteractionContext Context(ITextChannel here)
    {
        var guild = new Mock<IGuild>();
        guild.Setup(g => g.GetChannelAsync(HereId, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>())).ReturnsAsync(here);
        guild.Setup(g => g.GetChannelAsync(ElsewhereId, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync((IGuildChannel?) null);

        return Mock.Of<IInteractionContext>(c => c.Guild == guild.Object);
    }
}
