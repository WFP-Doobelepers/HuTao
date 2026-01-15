using System;
using System.Linq;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;

namespace HuTao.Services.CommandHelp;

public static class HelpBrowserRenderer
{
    private const uint AccentColor = 0x9B59FF;

    public static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<HelpBrowserState>();
        var disabled = p.ShouldDisable();

        var headerText = state.View switch
        {
            HelpBrowserView.Modules => "## Help • Modules\nSelect a module to view its commands.",
            HelpBrowserView.ModuleCommands => "## Help • Commands\nSelect a command for full details.",
            HelpBrowserView.CommandDetail => "## Help • Command\nFull details below.",
            _ => "## Help"
        };

        var container = new ContainerBuilder()
            .WithTextDisplay(headerText)
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        if (!string.IsNullOrWhiteSpace(state.Notice))
        {
            container.WithTextDisplay($"-# {state.Notice}");
            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        switch (state.View)
        {
            case HelpBrowserView.Modules:
                AddModules(container, state, p);
                break;
            case HelpBrowserView.ModuleCommands:
                AddCommands(container, state, p);
                break;
            case HelpBrowserView.CommandDetail:
                AddCommandDetail(container, state);
                break;
        }

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        container.WithActionRow(new ActionRowBuilder()
            .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
            .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
            .AddNextButton(p, "▶", ButtonStyle.Secondary));

        container.WithActionRow(new ActionRowBuilder()
            .WithButton("Search", HelpBrowserComponentIds.SearchButtonId, ButtonStyle.Primary, disabled: disabled)
            .WithButton("Back", HelpBrowserComponentIds.BackButtonId, ButtonStyle.Secondary,
                disabled: disabled || state.View is HelpBrowserView.Modules)
            .AddStopButton(p, "Close", ButtonStyle.Danger));

        var filterText = state.View is HelpBrowserView.Modules
            ? $" • Filter: {(string.IsNullOrWhiteSpace(state.TagFilter) ? "All" : state.TagFilter)}"
            : string.Empty;

        container
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay($"-# Prefix: `{state.Prefix}`{filterText} • Page {p.CurrentPageIndex + 1} of {p.PageCount}")
            .WithAccentColor(AccentColor);

        return new PageBuilder()
            .WithComponents(new ComponentBuilderV2().WithContainer(container).Build())
            .Build();
    }

    private static void AddModules(ContainerBuilder container, HelpBrowserState state, IComponentPaginator p)
    {
        var tags = state.GetAvailableTags();
        if (tags.Count > 0)
        {
            var tagOptions = tags
                .Select(t => new SelectMenuOptionBuilder(t.Truncate(100), t, isDefault: t.Equals(state.TagFilter, StringComparison.OrdinalIgnoreCase)))
                .Prepend(new SelectMenuOptionBuilder("All Tags", "__all__", "Show all modules",
                    isDefault: string.IsNullOrWhiteSpace(state.TagFilter)))
                .ToList();

            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(HelpBrowserComponentIds.TagSelectId)
                    .WithPlaceholder("Filter by tag…")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithOptions(tagOptions)
                    .WithDisabled(p.ShouldDisable())));

            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        var filtered = state.GetFilteredModules();
        var (start, endExclusive) = state.GetPageSlice(p.CurrentPageIndex);
        var pageModules = filtered.Skip(start).Take(endExclusive - start).ToList();

        if (pageModules.Count == 0)
        {
            container.WithTextDisplay("-# No modules.");
            return;
        }

        var list = string.Join("\n", pageModules.Select((m, i) => $"**{i + 1}.** {m.Module.Name}"));
        container.WithTextDisplay(list.Truncate(3200));

        var options = pageModules.Select(m =>
        {
            var tagsText = m.Module.HelpTags.Count > 0 ? string.Join(", ", m.Module.HelpTags.Take(4)) : "No tags";
            return new SelectMenuOptionBuilder(
                m.Module.Name.Truncate(100),
                m.Index.ToString(),
                description: tagsText.Truncate(100));
        }).ToList();

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(HelpBrowserComponentIds.ModuleSelectId)
                .WithPlaceholder("Select a module…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(options)
                .WithDisabled(p.ShouldDisable())));
    }

    private static void AddCommands(ContainerBuilder container, HelpBrowserState state, IComponentPaginator p)
    {
        var module = state.GetSelectedModule();
        if (module is null)
        {
            container.WithTextDisplay("-# No module selected.");
            return;
        }

        var commands = module.Commands
            .OrderBy(c => c.Aliases.FirstOrDefault() ?? c.Name)
            .ToList();

        container.WithTextDisplay($"### Module: {module.Name}\n{module.Summary ?? "No summary."}".Truncate(800));
        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        var (start, endExclusive) = state.GetPageSlice(p.CurrentPageIndex);
        var pageCommands = commands.Skip(start).Take(endExclusive - start).ToList();

        if (pageCommands.Count == 0)
        {
            container.WithTextDisplay("-# No commands.");
            return;
        }

        var list = string.Join("\n", pageCommands.Select((c, i) =>
        {
            var name = c.Aliases.FirstOrDefault() ?? c.Name;
            return $"**{i + 1}.** `{name}` — {(c.Summary ?? "No summary.").Truncate(80)}";
        }));

        container.WithTextDisplay(list.Truncate(3200));

        var options = pageCommands.Select((c, i) =>
        {
            var index = start + i;
            var name = c.Aliases.FirstOrDefault() ?? c.Name;
            return new SelectMenuOptionBuilder(
                name.Truncate(100),
                index.ToString(),
                description: (c.Summary ?? "No summary.").Truncate(100));
        }).ToList();

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(HelpBrowserComponentIds.CommandSelectId)
                .WithPlaceholder("Select a command…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(options)
                .WithDisabled(p.ShouldDisable())));
    }

    private static void AddCommandDetail(ContainerBuilder container, HelpBrowserState state)
    {
        var module = state.GetSelectedModule();
        var commands = state.GetSelectedCommands();
        if (module is null || commands is null || state.SelectedCommandIndex is null)
        {
            container.WithTextDisplay("-# No command selected.");
            return;
        }

        var command = commands[state.SelectedCommandIndex.Value];
        var embed = command.ToEmbedBuilder().Build();
        container.WithTextDisplay(embed.ToComponentsV2Text(maxChars: 3800));
    }
}

