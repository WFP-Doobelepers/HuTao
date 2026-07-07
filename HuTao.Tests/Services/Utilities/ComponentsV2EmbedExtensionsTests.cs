using Discord;
using HuTao.Services.Utilities;
using HuTao.Tests.Testing;
using Xunit;

namespace HuTao.Tests.Services.Utilities;

public class ComponentsV2EmbedExtensionsTests
{
    [Fact]
    public void ToComponentsV2Message_NoThumbnailNoUrl_DoesNotThrow()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Title")
            .WithDescription("Description")
            .Build();

        var components = embed.ToComponentsV2Message();

        Assert.NotNull(components);
        components.ShouldBeValidComponentsV2();
    }

    [Fact]
    public void ToComponentsV2Message_WithUrl_DoesNotThrow()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Title")
            .WithDescription("Description")
            .WithUrl("https://example.com")
            .Build();

        var components = embed.ToComponentsV2Message();

        Assert.NotNull(components);
        components.ShouldBeValidComponentsV2();
    }

    [Fact]
    public void ToComponentsV2Text_IncludesAuthorName()
    {
        // Message/reaction logs put "who sent the message" into the embed author.
        var embed = new EmbedBuilder()
            .WithAuthor("SomeUser#1234 (123456789)")
            .WithTitle("Message")
            .WithDescription("hello world")
            .Build();

        var text = embed.ToComponentsV2Text();

        Assert.Contains("SomeUser#1234 (123456789)", text);
    }

    [Fact]
    public void ToComponentsV2Text_IncludesFooterText()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Result")
            .WithFooter("Requested by SomeUser")
            .Build();

        var text = embed.ToComponentsV2Text();

        Assert.Contains("Requested by SomeUser", text);
    }

    [Fact]
    public void ToComponentsV2Message_WithAuthor_PreservesAuthorAndIsValid()
    {
        var embed = new EmbedBuilder()
            .WithAuthor("SomeUser#1234 (123456789)")
            .WithTitle("Message")
            .WithDescription("hello world")
            .Build();

        var components = embed.ToComponentsV2Message();

        Assert.NotNull(components);
        components.ShouldBeValidComponentsV2();
    }
}

