using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Fergun.Interactive.Pagination;
using HuTao.Services.CommandHelp;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using HuTao.Tests.Testing;
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
        Assert.NotNull(page.Components);
        page.Components.ShouldBeValidComponentsV2();
    }

    [Fact]
    public void HelpBrowserRenderer_GeneratePage_WithLongNotice_TruncatesNoticeToComponentsV2Limit()
    {
        var (_, paginator) = Create(s =>
        {
            s.View = HelpBrowserView.Modules;
            s.Notice = new string('a', 5000);
        });

        var page = HelpBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
        Assert.NotNull(page.Components);
        page.Components.ShouldBeValidComponentsV2();
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
        Assert.NotNull(page.Components);
        page.Components.ShouldBeValidComponentsV2();
    }

    [Theory]
    [InlineData(HelpBrowserView.Modules)]
    [InlineData(HelpBrowserView.ModuleCommands)]
    [InlineData(HelpBrowserView.CommandDetail)]
    public void HelpBrowserRenderer_GeneratePage_AllViews_ProduceValidComponents(HelpBrowserView view)
    {
        var (state, paginator) = Create(s =>
        {
            s.View = view;
            if (view == HelpBrowserView.ModuleCommands || view == HelpBrowserView.CommandDetail)
            {
                s.SelectedModuleIndex = 0;
                if (view == HelpBrowserView.CommandDetail)
                    s.SelectedCommandIndex = 0;
            }
        });

        var page = HelpBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
        Assert.NotNull(page.Components);
        page.Components.ShouldBeValidComponentsV2();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(3000)]
    public void HelpBrowserRenderer_GeneratePage_WithVaryingNoticeLengths_ProducesValidComponents(int noticeLength)
    {
        var (_, paginator) = Create(s =>
        {
            s.View = HelpBrowserView.Modules;
            s.Notice = noticeLength > 0 ? new string('n', noticeLength) : null;
        });

        var page = HelpBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
        Assert.NotNull(page.Components);
        page.Components.ShouldBeValidComponentsV2();
    }

    [Fact]
    public void HelpBrowserRenderer_GeneratePage_ComponentCount_NeverExceeds40()
    {
        var modules = CreateManyModules(20);
        var state = HelpBrowserState.Create(modules, "h!");
        state.View = HelpBrowserView.Modules;

        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(1);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(user.Object)
            .WithPageCount(state.GetPageCount())
            .WithUserState(state)
            .WithPageFactory(HelpBrowserRenderer.GeneratePage)
            .Build();

        for (var pageIndex = 0; pageIndex < state.GetPageCount(); pageIndex++)
        {
            paginator.SetPage(pageIndex);
            var page = HelpBrowserRenderer.GeneratePage(paginator);

            Assert.NotNull(page.Components);
            var componentCount = ComponentsV2Assertions.CountAllComponents(page.Components);
            Assert.True(componentCount <= 40,
                $"Page {pageIndex} has {componentCount} components, exceeding the 40 limit.");
        }
    }

    [Fact]
    public void HelpBrowserRenderer_GeneratePage_Validation_AlwaysPasses()
    {
        foreach (var view in Enum.GetValues<HelpBrowserView>())
        {
            var (state, paginator) = Create(s =>
            {
                s.View = view;
                if (view == HelpBrowserView.ModuleCommands || view == HelpBrowserView.CommandDetail)
                {
                    s.SelectedModuleIndex = 0;
                    if (view == HelpBrowserView.CommandDetail)
                        s.SelectedCommandIndex = 0;
                }
            });

            var page = HelpBrowserRenderer.GeneratePage(paginator);

            Assert.NotNull(page.Components);
            var result = ComponentsV2Validator.Validate(page.Components);
            Assert.True(result.IsValid, $"View {view} validation failed: {result}");
        }
    }

    private static IReadOnlyCollection<ModuleHelpData> CreateManyModules(int count)
    {
        return Enumerable.Range(0, count).Select(i => new ModuleHelpData
        {
            Name = $"Module{i}",
            Summary = $"Summary for module {i}.",
            HelpTags = new[] { $"tag{i}" },
            Commands = new List<CommandHelpData>
            {
                new()
                {
                    Name = $"command{i}",
                    Summary = $"Command {i} summary.",
                    Aliases = new[] { $"cmd{i}" },
                    Parameters = Array.Empty<ParameterHelpData>()
                }
            }
        }).ToArray();
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

