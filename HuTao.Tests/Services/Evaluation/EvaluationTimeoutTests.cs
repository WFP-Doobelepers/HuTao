using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Services.Evaluation;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Evaluation;

public class EvaluationTimeoutTests
{
    private sealed class TestContext() : Context(
        Mock.Of<IDiscordClient>(), Mock.Of<IGuild>(), Mock.Of<IMessageChannel>(), Mock.Of<IUser>())
    {
        public override Task ReplyAsync(
            string? message = null, bool isTTS = false, Embed? embed = null, RequestOptions? options = null,
            AllowedMentions? allowedMentions = null, MessageReference? messageReference = null,
            MessageComponent? components = null, ISticker[]? stickers = null, Embed[]? embeds = null,
            MessageFlags flags = MessageFlags.None, bool ephemeral = false)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task ScriptThatNeverFinishes_ReturnsATimeoutResult()
    {
        var service = new EvaluationService(Mock.Of<IServiceProvider>());
        var watch = Stopwatch.StartNew();

        var result = await service.EvaluateAsync(new TestContext(), "while (true) { }", TimeSpan.FromSeconds(1));

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), $"Took {watch.Elapsed}.");
        Assert.Equal(nameof(TimeoutException), result.ExceptionType);
        Assert.Contains("did not finish", result.Exception);
    }

    [Fact]
    public async Task ScriptThatFinishesInTime_ReturnsItsValue()
    {
        var service = new EvaluationService(Mock.Of<IServiceProvider>());

        var result = await service.EvaluateAsync(new TestContext(), "1 + 1", TimeSpan.FromSeconds(30));

        Assert.Null(result.Exception);
        Assert.Equal(2, result.ReturnValue);
    }
}
