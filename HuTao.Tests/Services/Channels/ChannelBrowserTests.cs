using System;
using System.Collections.Generic;
using Discord;
using Fergun.Interactive.Pagination;
using HuTao.Services.Channels;
using HuTao.Services.Interactive.Paginator;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Channels;

public class ChannelBrowserTests
{
    [Fact]
    public void ChannelBrowserRenderer_GeneratePage_List_DoesNotThrow()
    {
        var state = ChannelBrowserState.Create("Test", CreateEntries());
        state.View = ChannelBrowserView.List;

        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(1);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(user.Object)
            .WithUserState(state)
            .WithPageCount(state.GetPageCount())
            .WithPageFactory(ChannelBrowserRenderer.GeneratePage)
            .Build();

        var page = ChannelBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
    }

    [Fact]
    public void ChannelBrowserRenderer_GeneratePage_Detail_DoesNotThrow()
    {
        var entries = new List<ChannelEntry>(CreateEntries());
        var state = ChannelBrowserState.Create("Test", entries);
        state.Select(entries[0].Id);

        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(1);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(user.Object)
            .WithUserState(state)
            .WithPageCount(state.GetPageCount())
            .WithPageFactory(ChannelBrowserRenderer.GeneratePage)
            .Build();

        var page = ChannelBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
    }

    private static IReadOnlyCollection<ChannelEntry> CreateEntries()
    {
        return new[]
        {
            new ChannelEntry(
                Id: 1,
                Name: "general",
                Kind: ChannelKind.Text,
                CategoryId: 10,
                Position: 5,
                IsNsfw: false,
                SlowmodeSeconds: 0,
                UserLimit: null,
                Topic: "Hello"),
            new ChannelEntry(
                Id: 2,
                Name: "voice",
                Kind: ChannelKind.Voice,
                CategoryId: 10,
                Position: 4,
                IsNsfw: false,
                SlowmodeSeconds: null,
                UserLimit: 2,
                Topic: null),
            new ChannelEntry(
                Id: 10,
                Name: "category",
                Kind: ChannelKind.Category,
                CategoryId: null,
                Position: 99,
                IsNsfw: false,
                SlowmodeSeconds: null,
                UserLimit: null,
                Topic: null)
        };
    }
}

