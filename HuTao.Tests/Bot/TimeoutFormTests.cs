using System;
using System.Threading.Tasks;
using Discord;
using HuTao.Bot.Modules.Moderation;
using Moq;
using Xunit;

namespace HuTao.Tests.Bot;

// The Length field of the timeout form is optional, so a moderator can send the form without a length.
public class TimeoutFormTests
{
    [Fact]
    public async Task TimeoutForm_WithoutALength_GoesOnToTheTimeout()
    {
        var module = new Probe();
        var form = new InteractiveModerationModule.TimeoutModal { Length = null };

        var error = await Record.ExceptionAsync(() => module.TimeoutAsync(Mock.Of<IGuildUser>(), form));

        Assert.IsType<Probe.Reached>(error);
    }

    // Stops the handler at its first reply, which the timeout command sends before it times the member out.
    private sealed class Probe() : InteractiveModerationModule(null!, null!, null!)
    {
        protected override Task DeferAsync(bool ephemeral = false, RequestOptions? options = null)
            => throw new Reached();

        public sealed class Reached : Exception;
    }
}
