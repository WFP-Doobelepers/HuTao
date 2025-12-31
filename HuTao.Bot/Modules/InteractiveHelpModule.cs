using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data.Config;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules;

[RequireContext(ContextType.Guild)]
public sealed class InteractiveHelpModule(ICommandHelpService help, InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    private const string ModuleSelectId = "help:module";
    private const string CommandSelectId = "help:command";
    private const string BackButtonId = "help:back";

    [SlashCommand("help", "Browse bot commands and usage.")]
    public async Task HelpAsync(
        [Summary("Search by module/tag/command name (optional).")]
        [Autocomplete(typeof(HelpAutocomplete))]
        string? query = null,
        [Summary("True to only show you the help.")]
        [RequireEphemeralScope]
        bool ephemeral = false)
    {
        await DeferAsync(ephemeral);

        if (!string.IsNullOrWhiteSpace(query))
        {
            if (help.TryGetPaginator(query, HelpDataType.Command | HelpDataType.Module, out var paginatorBuilder))
            {
                await interactive.SendPaginatorAsync(
                    paginatorBuilder.WithUsers(Context.User).Build(),
                    Context.Interaction,
                    ephemeral: ephemeral,
                    timeout: TimeSpan.FromMinutes(10),
                    resetTimeoutOnInput: true,
                    responseType: InteractionResponseType.DeferredChannelMessageWithSource);
                return;
            }

            var notFound = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay($"## Help\nNo results for `{FormatUtilities.SanitizeAllMentions(query)}`.")
                    .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay("-# Tip: try a module name, a help tag, or a command name.")
                    .WithAccentColor(AccentColor))
                .Build();

            await FollowupAsync(components: notFound, ephemeral: ephemeral, allowedMentions: AllowedMentions.None);
            return;
        }

        var state = HelpBrowserState.Create(help.GetModuleHelpData(), HuTaoConfig.Configuration.Prefix);

        var browser = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(state.GetPageCount())
            .WithUserState(state)
            .WithPageFactory(GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            browser,
            Context.Interaction,
            ephemeral: ephemeral,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    [ComponentInteraction(ModuleSelectId, true)]
    public async Task SelectModuleAsync(string moduleIndex)
    {
        if (!TryGetBrowser(out var paginator, out var state, out _))
            return;

        if (!int.TryParse(moduleIndex, out var index) || index < 0 || index >= state.Modules.Count)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.View = HelpView.ModuleCommands;
        state.SelectedModuleIndex = index;
        state.SelectedCommandIndex = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(CommandSelectId, true)]
    public async Task SelectCommandAsync(string commandIndex)
    {
        if (!TryGetBrowser(out var paginator, out var state, out _))
            return;

        var commands = state.GetSelectedCommands();
        if (commands is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (!int.TryParse(commandIndex, out var index) || index < 0 || index >= commands.Count)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.View = HelpView.CommandDetail;
        state.SelectedCommandIndex = index;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(BackButtonId, true)]
    public async Task BackAsync()
    {
        if (!TryGetBrowser(out var paginator, out var state, out _))
            return;

        state.View = state.View switch
        {
            HelpView.CommandDetail => HelpView.ModuleCommands,
            HelpView.ModuleCommands => HelpView.Modules,
            _ => HelpView.Modules
        };

        state.SelectedCommandIndex = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    private bool TryGetBrowser(out IComponentPaginator paginator, out HelpBrowserState state, out IComponentInteraction interaction)
    {
        if (Context.Interaction is not IComponentInteraction i
            || !interactive.TryGetComponentPaginator(i.Message, out var p)
            || p is null
            || !p.CanInteract(i.User))
        {
            paginator = null!;
            state = null!;
            interaction = null!;
            return false;
        }

        interaction = i;
        paginator = p;
        state = paginator.GetUserState<HelpBrowserState>();
        return true;
    }

    private async Task RenderAsync(IComponentPaginator paginator)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(Context.Interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    private static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<HelpBrowserState>();
        var disabled = p.ShouldDisable();

        var container = new ContainerBuilder()
            .WithTextDisplay(state.View switch
            {
                HelpView.Modules => "## Help • Modules\nSelect a module to view its commands.",
                HelpView.ModuleCommands => "## Help • Commands\nSelect a command for full details.",
                HelpView.CommandDetail => "## Help • Command\nFull details below.",
                _ => "## Help"
            })
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        switch (state.View)
        {
            case HelpView.Modules:
                AddModules(container, state, p);
                break;
            case HelpView.ModuleCommands:
                AddCommands(container, state, p);
                break;
            case HelpView.CommandDetail:
                AddCommandDetail(container, state);
                break;
        }

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        var nav = new ActionRowBuilder()
            .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
            .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
            .AddNextButton(p, "▶", ButtonStyle.Secondary);

        container.WithActionRow(nav);

        var controls = new ActionRowBuilder()
            .WithButton("Back", BackButtonId, ButtonStyle.Secondary, disabled: disabled || state.View is HelpView.Modules)
            .AddStopButton(p, "Close", ButtonStyle.Danger);

        container.WithActionRow(controls);

        container
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay($"-# Prefix: `{state.Prefix}` • Page {p.CurrentPageIndex + 1} of {p.PageCount}")
            .WithAccentColor(AccentColor);

        return new PageBuilder()
            .WithComponents(new ComponentBuilderV2().WithContainer(container).Build())
            .Build();
    }

    private static void AddModules(ContainerBuilder container, HelpBrowserState state, IComponentPaginator p)
    {
        var (start, endExclusive) = state.GetPageSlice(p.CurrentPageIndex);
        var pageModules = state.Modules.Skip(start).Take(endExclusive - start).ToList();

        if (pageModules.Count == 0)
        {
            container.WithTextDisplay("-# No modules.");
            return;
        }

        var list = string.Join("\n", pageModules.Select((m, i) => $"**{i + 1}.** {m.Name}"));
        container.WithTextDisplay(list.Truncate(3200));

        var options = pageModules.Select((m, i) =>
        {
            var index = start + i;
            var tags = m.HelpTags.Count > 0 ? string.Join(", ", m.HelpTags.Take(4)) : "No tags";
            return new SelectMenuOptionBuilder(
                m.Name.Truncate(100),
                index.ToString(),
                description: tags.Truncate(100));
        }).ToList();

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(ModuleSelectId)
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
                .WithCustomId(CommandSelectId)
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

    private enum HelpView
    {
        Modules,
        ModuleCommands,
        CommandDetail
    }

    private sealed class HelpBrowserState
    {
        private const int PageSize = 10;

        public required string Prefix { get; init; }
        public required IReadOnlyList<ModuleHelpData> Modules { get; init; }
        public HelpView View { get; set; } = HelpView.Modules;

        public int? SelectedModuleIndex { get; set; }
        public int? SelectedCommandIndex { get; set; }

        public static HelpBrowserState Create(IReadOnlyCollection<ModuleHelpData> modules, string prefix)
        {
            var ordered = modules.OrderBy(m => m.Name).ToList();
            return new HelpBrowserState
            {
                Prefix = prefix,
                Modules = ordered
            };
        }

        public int GetPageCount()
        {
            return View switch
            {
                HelpView.Modules => Math.Max(1, (int)Math.Ceiling((double)Modules.Count / PageSize)),
                HelpView.ModuleCommands => Math.Max(1, (int)Math.Ceiling((double)(GetSelectedModule()?.Commands.Count ?? 0) / PageSize)),
                _ => 1
            };
        }

        public (int Start, int EndExclusive) GetPageSlice(int pageIndex)
        {
            var start = pageIndex * PageSize;
            var end = start + PageSize;
            return (start, end);
        }

        public ModuleHelpData? GetSelectedModule()
        {
            if (SelectedModuleIndex is null) return null;
            var index = SelectedModuleIndex.Value;
            return index >= 0 && index < Modules.Count ? Modules[index] : null;
        }

        public IReadOnlyList<CommandHelpData>? GetSelectedCommands()
        {
            var module = GetSelectedModule();
            return module?.Commands
                .OrderBy(c => c.Aliases.FirstOrDefault() ?? c.Name)
                .ToList();
        }
    }
}

