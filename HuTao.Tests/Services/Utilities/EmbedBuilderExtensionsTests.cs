using System.Collections.Generic;
using System.Linq;
using Discord;
using HuTao.Services.Utilities;
using Xunit;

namespace HuTao.Tests.Services.Utilities;

public class EmbedBuilderExtensionsTests
{
    [Fact]
    public void AddItemsIntoFields_DoesNotExceedMaxFieldCount_WhenManyItems()
    {
        var builder = new EmbedBuilder()
            .WithTitle("Test")
            .WithDescription("Description");

        var items = Enumerable.Range(0, 200)
            .Select(i => $"{i}: {new string('x', 900)}")
            .ToList();

        builder.AddItemsIntoFields("Items", items);

        Assert.True(builder.Fields.Count <= EmbedBuilder.MaxFieldCount);
    }

    [Fact]
    public void AddItemsIntoFields_RespectsExistingFields()
    {
        var builder = new EmbedBuilder()
            .WithTitle("Test");

        for (var i = 0; i < EmbedBuilder.MaxFieldCount - 1; i++)
        {
            builder.AddField($"Field {i}", "Value");
        }

        builder.AddItemsIntoFields("Items", new List<string> { "One", "Two", "Three" });

        var embed = builder.Build();

        Assert.Equal(EmbedBuilder.MaxFieldCount, embed.Fields.Length);
    }
}

