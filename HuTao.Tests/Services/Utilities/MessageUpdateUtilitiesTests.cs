using System;
using Discord;
using HuTao.Services.Utilities;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Utilities;

public class MessageUpdateUtilitiesTests
{
    [Fact]
    public void IsUserContentEdit_ReturnsFalse_WhenNewMessageIsNotUserMessage()
    {
        var newMessage = new Mock<IMessage>();
        Assert.False(MessageUpdateUtilities.IsUserContentEdit(newMessage.Object));
    }

    [Fact]
    public void IsUserContentEdit_ReturnsFalse_WhenEditedTimestampIsNull()
    {
        var newMessage = new Mock<IUserMessage>();
        newMessage.SetupGet(m => m.EditedTimestamp).Returns((DateTimeOffset?) null);

        Assert.False(MessageUpdateUtilities.IsUserContentEdit(newMessage.Object));
    }

    [Fact]
    public void IsUserContentEdit_ReturnsTrue_WhenEditedTimestampIsNotNull_AndOldMessageIsNull()
    {
        var newMessage = new Mock<IUserMessage>();
        newMessage.SetupGet(m => m.EditedTimestamp).Returns(DateTimeOffset.UtcNow);
        newMessage.SetupGet(m => m.Content).Returns("new");

        Assert.True(MessageUpdateUtilities.IsUserContentEdit(newMessage.Object));
    }

    [Fact]
    public void IsUserContentEdit_ReturnsFalse_WhenOldAndNewContentAreEqual()
    {
        var oldMessage = new Mock<IUserMessage>();
        oldMessage.SetupGet(m => m.Content).Returns("same");

        var newMessage = new Mock<IUserMessage>();
        newMessage.SetupGet(m => m.EditedTimestamp).Returns(DateTimeOffset.UtcNow);
        newMessage.SetupGet(m => m.Content).Returns("same");

        Assert.False(MessageUpdateUtilities.IsUserContentEdit(newMessage.Object, oldMessage.Object));
    }

    [Fact]
    public void IsUserContentEdit_ReturnsTrue_WhenOldAndNewContentDiffer()
    {
        var oldMessage = new Mock<IUserMessage>();
        oldMessage.SetupGet(m => m.Content).Returns("old");

        var newMessage = new Mock<IUserMessage>();
        newMessage.SetupGet(m => m.EditedTimestamp).Returns(DateTimeOffset.UtcNow);
        newMessage.SetupGet(m => m.Content).Returns("new");

        Assert.True(MessageUpdateUtilities.IsUserContentEdit(newMessage.Object, oldMessage.Object));
    }
}

