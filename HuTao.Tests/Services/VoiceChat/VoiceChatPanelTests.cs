using System;
using Discord;
using Fergun.Interactive.Pagination;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.VoiceChat;
using HuTao.Tests.Testing;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.VoiceChat;

public class VoiceChatPanelTests
{
    [Fact]
    public void VoiceChatPanelRenderer_GeneratePage_DoesNotThrow()
    {
        var state = VoiceChatPanelState.Create("Test", 1, 2, 3);
        state.UpdateStatus(locked: true, hidden: false, userLimit: 2);

        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(1);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(user.Object)
            .WithUserState(state)
            .WithPageCount(1)
            .WithPageFactory(VoiceChatPanelRenderer.GeneratePage)
            .Build();

        var page = VoiceChatPanelRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
        Assert.NotNull(page.Components);
        page.Components.ShouldBeValidComponentsV2();
    }
}

