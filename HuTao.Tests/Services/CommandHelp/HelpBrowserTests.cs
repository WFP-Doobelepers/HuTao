using System;
using System.Collections.Generic;
using Discord;
using Fergun.Interactive.Pagination;
using HuTao.Services.CommandHelp;
using HuTao.Services.Interactive.Paginator;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.CommandHelp;

public class HelpBrowserTests
{
    [Fact]
    public void HelpBrowserRenderer_GeneratePage_Modules_DoesNotThrow()
    {
        var (state, paginator) = Create(state =>
        {
            state.View = HelpBrowserView.Modules;
            state.TagFilter = null;
        });

        var page = HelpBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
    }

    [Fact]
    public void HelpBrowserRenderer_GeneratePage_Modules_WithTagFilter_DoesNotThrow()
    {
        var (state, paginator) = Create(s =>
        {
            s.View = HelpBrowserView.Modules;
            s.TagFilter = "moderation";
        });

        var page = HelpBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
    }

    [Fact]
    public void HelpBrowserState_TryApplyQuery_TagExact_SetsModulesViewAndFilter()
    {
        var state = HelpBrowserState.Create(CreateModules(), "h!");

        var ok = state.TryApplyQuery("moderation");

        Assert.True(ok);
        Assert.Equal(HelpBrowserView.Modules, state.View);
        Assert.Equal("moderation", state.TagFilter);
    }

    [Fact]
    public void HelpBrowserState_TryApplyQuery_Command_SetsCommandDetail()
    {
        var state = HelpBrowserState.Create(CreateModules(), "h!");

        var ok = state.TryApplyQuery("ban");

        Assert.True(ok);
        Assert.Equal(HelpBrowserView.CommandDetail, state.View);
        Assert.NotNull(state.SelectedModuleIndex);
        Assert.NotNull(state.SelectedCommandIndex);
    }

    [Fact]
    public void HelpBrowserState_TryApplyQuery_ModuleOnly_DoesNotSelectCommand()
    {
        var state = HelpBrowserState.Create(CreateModules(), "h!");

        var ok = state.TryApplyQuery("ban", HelpDataType.Module);

        Assert.False(ok);
        Assert.Equal(HelpBrowserView.Modules, state.View);
        Assert.Null(state.SelectedModuleIndex);
        Assert.Null(state.SelectedCommandIndex);
    }

    private static (HelpBrowserState State, IComponentPaginator Paginator) Create(Action<HelpBrowserState> configure)
    {
        var state = HelpBrowserState.Create(CreateModules(), "h!");
        configure(state);

        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(1);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(user.Object)
            .WithPageCount(state.GetPageCount())
            .WithUserState(state)
            .WithPageFactory(HelpBrowserRenderer.GeneratePage)
            .Build();

        return (state, paginator);
    }

    private static IReadOnlyCollection<ModuleHelpData> CreateModules()
    {
        var moderation = new ModuleHelpData
        {
            Name = "Moderation",
            Summary = "Moderation commands.",
            HelpTags = new[] { "moderation" },
            Commands = new List<CommandHelpData>
            {
                new()
                {
                    Name = "ban",
                    Summary = "Ban a user.",
                    Aliases = new[] { "ban" },
                    Parameters = Array.Empty<ParameterHelpData>()
                }
            }
        };

        var logging = new ModuleHelpData
        {
            Name = "Logging",
            Summary = "Logging commands.",
            HelpTags = new[] { "logging" },
            Commands = new List<CommandHelpData>
            {
                new()
                {
                    Name = "log",
                    Summary = "Configure logs.",
                    Aliases = new[] { "log" },
                    Parameters = Array.Empty<ParameterHelpData>()
                }
            }
        };

        return new[] { logging, moderation };
    }
}

