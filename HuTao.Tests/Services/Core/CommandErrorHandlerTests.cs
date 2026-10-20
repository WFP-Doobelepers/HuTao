using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Services.Core.Listeners;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core;

public class CommandErrorHandlerTests
{
    private sealed class FakeClock : Microsoft.Extensions.Internal.ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }

    private static Mock<IUserMessage> Message(ulong id)
    {
        var message = new Mock<IUserMessage>();
        message.SetupGet(m => m.Id).Returns(id);
        return message;
    }

    [Fact]
    public async Task AssociatedError_IsKeptForTheMessageAndReactedOnce()
    {
        var handler = new CommandErrorHandler(new DiscordSocketClient(), new MemoryCache(new MemoryCacheOptions()));
        var message = Message(1);

        await handler.AssociateError(message.Object, "first");
        await handler.AssociateError(message.Object, "second");

        Assert.True(handler.TryGetError(1, out var error));
        Assert.Equal("first", error);
        message.Verify(m => m.AddReactionAsync(It.IsAny<IEmote>(), It.IsAny<RequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task AssociatedError_IsDroppedAfterAnHour()
    {
        var clock = new FakeClock();
        var handler = new CommandErrorHandler(
            new DiscordSocketClient(), new MemoryCache(new MemoryCacheOptions { Clock = clock }));

        await handler.AssociateError(Message(1).Object, "boom");
        Assert.True(handler.TryGetError(1, out _));

        clock.UtcNow += CommandErrorHandler.ErrorLifetime + TimeSpan.FromMinutes(1);

        Assert.False(handler.TryGetError(1, out _));
    }

    [Fact]
    public void OnlyExceptionsAreLoggedAsErrors()
    {
        Assert.Equal(LogLevel.Error, UserFacingError.LevelFor(true));
        Assert.Equal(LogLevel.Debug, UserFacingError.LevelFor(false));
    }
}
