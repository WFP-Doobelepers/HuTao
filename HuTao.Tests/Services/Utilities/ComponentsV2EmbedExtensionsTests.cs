using Discord;
using HuTao.Services.Utilities;
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
    }
}

