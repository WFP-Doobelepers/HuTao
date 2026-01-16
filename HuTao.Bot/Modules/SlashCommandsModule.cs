using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules;

[RequireContext(ContextType.Guild)]
public sealed class SlashCommandsModule(
    InteractionService slashCommands,
    InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    private const string CommandSelectId = "slashhelp:cmd";
    private const string BackButtonId = "slashhelp:back";

    [SlashCommand("commands", "Browse slash commands and usage.")]
    public async Task CommandsAsync(
        [Summary(description: "Search by name (optional).")]
        [Autocomplete(typeof(SlashCommandAutocomplete))]
        string? query = null,
        [Summary(description: "True to only show you the help.")]
        [RequireEphemeralScope]
        bool ephemeral = false)
    {
        await DeferAsync(ephemeral);

        var entries = slashCommands.SlashCommands
            .Select(c => new SlashEntry(GetFullName(c), c))
            .GroupBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var match = entries.FirstOrDefault(e => e.FullName.Equals(query, StringComparison.OrdinalIgnoreCase))
                ?? entries.FirstOrDefault(e => e.FullName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                ?? entries.FirstOrDefault(e => e.FullName.Contains(query, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                var notFound = new ComponentBuilderV2()
                    .WithContainer(new ContainerBuilder()
                        .WithTextDisplay($"## Slash Commands\nNo results for `{FormatUtilities.SanitizeAllMentions(query)}`.")
                        .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                        .WithTextDisplay("-# Tip: try the top-level name, e.g. `config`, `log`, `trigger`.")
                        .WithAccentColor(AccentColor))
                    .Build();

                await FollowupAsync(components: notFound, ephemeral: ephemeral, allowedMentions: AllowedMentions.None);
                return;
            }

            var state = new SlashHelpState(entries)
            {
                View = SlashHelpView.Detail,
                SelectedIndex = entries.FindIndex(e => e.FullName.Equals(match.FullName, StringComparison.OrdinalIgnoreCase))
            };

            var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
                .WithUsers(Context.User)
                .WithUserState(state)
                .WithPageCount(state.GetPageCount())
                .WithPageFactory(GeneratePage)
                .Build();

            await interactive.SendPaginatorAsync(
                paginator,
                Context.Interaction,
                ephemeral: ephemeral,
                timeout: TimeSpan.FromMinutes(10),
                resetTimeoutOnInput: true,
                responseType: InteractionResponseType.DeferredChannelMessageWithSource);

            return;
        }

        var listState = new SlashHelpState(entries);
        var browser = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithUserState(listState)
            .WithPageCount(listState.GetPageCount())
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

    [ComponentInteraction(CommandSelectId, true)]
    public async Task SelectCommandAsync(string commandIndex)
    {
        if (!TryGetBrowser(out var paginator, out var state, out var interaction))
            return;

        if (!int.TryParse(commandIndex, out var index) || index < 0 || index >= state.Commands.Count)
        {
            await DeferAsync();
            await RenderAsync(paginator, interaction);
            return;
        }

        state.View = SlashHelpView.Detail;
        state.SelectedIndex = index;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator, interaction);
    }

    [ComponentInteraction(BackButtonId, true)]
    public async Task BackAsync()
    {
        if (!TryGetBrowser(out var paginator, out var state, out var interaction))
            return;

        state.View = SlashHelpView.List;
        state.SelectedIndex = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator, interaction);
    }

    private bool TryGetBrowser(out IComponentPaginator paginator, out SlashHelpState state, out IComponentInteraction interaction)
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

        paginator = p;
        interaction = i;
        state = paginator.GetUserState<SlashHelpState>();
        return true;
    }

    private async Task RenderAsync(IComponentPaginator paginator, IComponentInteraction interaction)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    private static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<SlashHelpState>();
        var disabled = p.ShouldDisable();

        var container = new ContainerBuilder()
            .WithTextDisplay(state.View switch
            {
                SlashHelpView.List => "## Slash Commands\nSelect a command to view details.",
                SlashHelpView.Detail => "## Slash Command\nFull details below.",
                _ => "## Slash Commands"
            })
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        if (state.View is SlashHelpView.Detail)
        {
            var selected = state.GetSelected();
            if (selected is null)
            {
                container.WithTextDisplay("-# No command selected.");
            }
            else
            {
                container.WithTextDisplay(BuildCommandDetail(selected).Truncate(3800));
            }

            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

            container.WithActionRow(new ActionRowBuilder()
                .WithButton("Back", BackButtonId, ButtonStyle.Secondary, disabled: disabled)
                .AddStopButton(p, "Close", ButtonStyle.Danger));
        }
        else
        {
            var pageItems = state.GetPage(p.CurrentPageIndex);
            if (pageItems.Count == 0)
            {
                container.WithTextDisplay("-# No slash commands.");
            }
            else
            {
                var list = string.Join("\n", pageItems.Select((e, i) =>
                    $"**{i + 1}.** `/{e.FullName}` — {(e.Command.Description ?? "No description.").Truncate(80)}"));
                container.WithTextDisplay(list.Truncate(3200));
            }

            var options = pageItems.Select((e, i) =>
            {
                var index = (p.CurrentPageIndex * SlashHelpState.PageSize) + i;
                return new SelectMenuOptionBuilder(
                    $"/{e.FullName}".Truncate(100),
                    index.ToString(),
                    description: (e.Command.Description ?? "No description.").Truncate(100));
            }).ToList();

            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(CommandSelectId)
                    .WithPlaceholder(options.Count == 0 ? "No commands" : "View command details…")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithOptions(options)
                    .WithDisabled(disabled || options.Count == 0)));

            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

            container.WithActionRow(new ActionRowBuilder()
                .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
                .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
                .AddNextButton(p, "▶", ButtonStyle.Secondary)
                .AddStopButton(p, "Close", ButtonStyle.Danger));
        }

        container.WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small);
        container.WithTextDisplay($"-# Page {p.CurrentPageIndex + 1} of {p.PageCount}")
            .WithAccentColor(AccentColor);

        return new PageBuilder()
            .WithComponents(new ComponentBuilderV2().WithContainer(container).Build())
            .Build();
    }

    private static string BuildCommandDetail(SlashEntry entry)
    {
        var sb = new StringBuilder()
            .AppendLine($"### /{entry.FullName}")
            .AppendLine(entry.Command.Description ?? "No description.");

        var parameters = entry.Command.Parameters.OfType<SlashCommandParameterInfo>().ToList();
        if (parameters.Count > 0)
        {
            sb.AppendLine()
                .AppendLine("### Parameters");

            foreach (var p in parameters)
            {
                var type = FriendlyType(p.ParameterType);
                var required = p.IsRequired ? "required" : "optional";
                var auto = p.IsAutocomplete ? " • autocomplete" : string.Empty;
                var desc = string.IsNullOrWhiteSpace(p.Description) ? "No description." : p.Description;

                sb.AppendLine($"- **{p.Name}** ({type}, {required}{auto})");
                sb.AppendLine($"  -# {desc.Truncate(180)}");
            }
        }
        else
        {
            sb.AppendLine()
                .AppendLine("-# No parameters.");
        }

        return sb.ToString();
    }

    private static string FriendlyType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
            return $"{FriendlyType(underlying)}?";

        if (type.IsArray)
            return $"{FriendlyType(type.GetElementType()!)}[]";

        if (type == typeof(string))
            return "string";
        if (type == typeof(bool))
            return "bool";
        if (type == typeof(int))
            return "int";
        if (type == typeof(uint))
            return "uint";
        if (type == typeof(long))
            return "long";
        if (type == typeof(ulong))
            return "ulong";
        if (type == typeof(TimeSpan))
            return "duration";

        if (type.IsEnum)
            return type.Name;

        if (type == typeof(IUser) || typeof(IUser).IsAssignableFrom(type))
            return "user";
        if (type == typeof(IRole) || typeof(IRole).IsAssignableFrom(type))
            return "role";
        if (type == typeof(IChannel) || typeof(IChannel).IsAssignableFrom(type))
            return "channel";

        return type.Name;
    }

    private static string GetFullName(SlashCommandInfo command)
    {
        var parts = new Stack<string>();
        parts.Push(command.Name);

        for (var module = command.Module; module is not null; module = module.Parent)
        {
            if (!string.IsNullOrWhiteSpace(module.SlashGroupName))
                parts.Push(module.SlashGroupName);
        }

        return string.Join(' ', parts);
    }

    private enum SlashHelpView
    {
        List,
        Detail
    }

    private sealed class SlashHelpState
    {
        public const int PageSize = 10;

        public SlashHelpState(IReadOnlyList<SlashEntry> commands)
        {
            Commands = commands;
        }

        public IReadOnlyList<SlashEntry> Commands { get; }

        public SlashHelpView View { get; set; } = SlashHelpView.List;

        public int? SelectedIndex { get; set; }

        public int GetPageCount()
            => View is SlashHelpView.List
                ? Math.Max(1, (int)Math.Ceiling((double)Commands.Count / PageSize))
                : 1;

        public IReadOnlyList<SlashEntry> GetPage(int pageIndex)
            => Commands.Skip(pageIndex * PageSize).Take(PageSize).ToList();

        public SlashEntry? GetSelected()
            => SelectedIndex is { } i && i >= 0 && i < Commands.Count ? Commands[i] : null;
    }

    private sealed record SlashEntry(string FullName, SlashCommandInfo Command);
}

