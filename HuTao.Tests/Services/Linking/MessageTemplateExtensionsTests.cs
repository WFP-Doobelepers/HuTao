using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Linking;
using HuTao.Tests.Testing;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Linking;

public class MessageTemplateExtensionsTests
{
    [Fact]
    public async Task SendMessageAsync_TruncatesTemplateContent_ToComponentsV2TextLimit()
    {
        var longContent = new string('x', 5000);

        var sourceChannel = new Mock<IMessageChannel>();
        sourceChannel.SetupGet(x => x.Id).Returns(123UL);

        var sourceMessage = new Mock<IMessage>();
        sourceMessage.SetupGet(x => x.Channel).Returns(sourceChannel.Object);
        sourceMessage.SetupGet(x => x.Id).Returns(456UL);
        sourceMessage.SetupGet(x => x.Content).Returns(longContent);
        sourceMessage.SetupGet(x => x.Attachments).Returns(System.Array.Empty<IAttachment>());
        sourceMessage.SetupGet(x => x.Embeds).Returns(System.Array.Empty<Embed>());

        var template = new MessageTemplate(sourceMessage.Object, options: null)
        {
            Id = System.Guid.NewGuid()
        };

        MessageComponent? captured = null;
        var destinationChannel = new Mock<IMessageChannel>();
        destinationChannel
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<Embed?>(),
                It.IsAny<RequestOptions?>(),
                It.IsAny<AllowedMentions?>(),
                It.IsAny<MessageReference?>(),
                It.IsAny<MessageComponent?>(),
                It.IsAny<ISticker[]?>(),
                It.IsAny<Embed[]?>(),
                It.IsAny<MessageFlags>(),
                It.IsAny<PollProperties?>()))
            .Callback<string?, bool, Embed?, RequestOptions?, AllowedMentions?, MessageReference?, MessageComponent?, ISticker[]?, Embed[]?, MessageFlags, PollProperties?>(
                (_, _, _, _, _, _, components, _, _, _, _) => captured = components)
            .ReturnsAsync(Mock.Of<IUserMessage>());

        await template.SendMessageAsync(destinationChannel.Object);

        Assert.NotNull(captured);
        captured.ShouldBeValidComponentsV2();
    }
}

