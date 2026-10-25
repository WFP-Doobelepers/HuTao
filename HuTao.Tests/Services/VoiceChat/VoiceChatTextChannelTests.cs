using System.Threading.Tasks;
using Discord;
using HuTao.Bot.Behaviors;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.VoiceChat;

public class VoiceChatTextChannelTests
{
    // Staff can delete a voice chat's text channel and leave its voice channel. A member who joins that voice channel
    // has no chat to be let into, and the handler goes on to purge the channel they left.
    [Fact]
    public async Task JoiningAVoiceChatWithoutItsTextChannel_DoesNotThrow()
        => await VoiceChatBehavior.LetIntoChatAsync(null, Mock.Of<IGuildUser>(), showJoinLeave: true);
}
