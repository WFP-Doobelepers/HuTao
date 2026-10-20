using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HuTao.Services.Core.Listeners;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core;

// A command that fails with an unexpected exception shows the member only an error ID, and the log keeps the whole
// exception under that ID, so the owner can find it.
public class ErrorIdLoggingTests
{
    private sealed class CapturingLogger : ILogger<CommandHandlingService>
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, exception, formatter(state, exception)));
    }

    private static async Task<(CommandErrorHandler Errors, CapturingLogger Log)> FailAsync(IResult result)
    {
        var errors = new CommandErrorHandler(new DiscordSocketClient(), new MemoryCache(new MemoryCacheOptions()));
        var log = new CapturingLogger();
        var service = new CommandHandlingService(errors, new CommandService(), new DiscordSocketClient(), null!, log, null!);

        var message = new Mock<IUserMessage>();
        message.SetupGet(m => m.Id).Returns(5UL);
        var context = Mock.Of<ICommandContext>(c => c.Message == message.Object
            && c.User == Mock.Of<IUser>(u => u.Id == 6) && c.Channel == Mock.Of<IMessageChannel>(ch => ch.Id == 7));

        var executed = typeof(CommandHandlingService).GetMethod("CommandExecutedAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        // Discord.Net names the command for every failure the handler reports; only an unknown command has none.
        await (Task) executed.Invoke(service, [new Optional<CommandInfo>(null!), context, result])!;

        return (errors, log);
    }

    [Fact]
    public async Task AnUnexpectedException_ShowsAnId_AndLogsTheWholeExceptionUnderIt()
    {
        var failure = new InvalidOperationException("Host=db;Password=hunter2");

        var (errors, log) = await FailAsync(ExecuteResult.FromError(failure));

        Assert.True(errors.TryGetError(5, out var shown));
        Assert.DoesNotContain("hunter2", shown);
        var id = Regex.Match(shown, "`([0-9a-f]{8})`").Groups[1].Value;
        Assert.NotEmpty(id);

        var entry = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
        Assert.Contains($"ErrorId={id}", entry.Message);
    }

    // A reason a command gives on purpose still reaches the member as it is, and is not an error in the log.
    [Fact]
    public async Task ADeliberateRefusal_ShowsItsText()
    {
        var (errors, log) = await FailAsync(ExecuteResult.FromError(new UserFacingException("A command with that name already exists.")));

        Assert.True(errors.TryGetError(5, out var shown));
        Assert.Equal("A command with that name already exists.", shown);
        Assert.Equal(LogLevel.Debug, Assert.Single(log.Entries).Level);
    }
}
