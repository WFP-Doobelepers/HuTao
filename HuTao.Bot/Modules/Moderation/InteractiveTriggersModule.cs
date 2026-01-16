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
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.Moderation;

[Group("trigger", "Manage reprimand triggers.")]
[RequireContext(ContextType.Guild)]
public sealed class InteractiveTriggersModule(
    HuTaoContext db,
    ModerationService moderation,
    IMemoryCache cache,
    InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    private const string TriggerSelectId = "trg:select";
    private const string CategorySelectId = "trg:category";
    private const string BackButtonId = "trg:back";
    private const string ToggleButtonId = "trg:toggle";
    private const string DeleteButtonId = "trg:delete";
    private const string RefreshButtonId = "trg:refresh";
    private const string DeleteLoudButtonId = "trg:confirm:loud";
    private const string DeleteSilentButtonId = "trg:confirm:silent";

    [SlashCommand("panel", "Open an interactive trigger manager.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task PanelAsync(
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        [RequireEphemeralScope]
        bool ephemeral = false)
    {
        await DeferAsync(ephemeral);

        var state = await TriggerPanelState.CreateAsync(db, Context.Guild, category);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(state.GetPageCount())
            .WithUserState(state)
            .WithPageFactory(GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: ephemeral,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    [ComponentInteraction(TriggerSelectId, true)]
    public async Task SelectTriggerAsync(string triggerId)
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        if (!Guid.TryParse(triggerId, out var id) || state.Triggers.All(t => t.Id != id))
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.View = TriggerView.Detail;
        state.SelectedTriggerId = id;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(CategorySelectId, true)]
    public async Task SelectCategoryAsync(string categoryId)
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.Filter = TriggerCategoryFilter.Parse(categoryId);
        state.View = TriggerView.List;
        state.SelectedTriggerId = null;
        state.LastUpdated = DateTimeOffset.UtcNow;

        await state.ReloadAsync(db, Context.Guild);
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(BackButtonId, true)]
    public async Task BackAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.View = TriggerView.List;
        state.SelectedTriggerId = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(RefreshButtonId, true)]
    public async Task RefreshAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.LastUpdated = DateTimeOffset.UtcNow;
        await state.ReloadAsync(db, Context.Guild);
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(ToggleButtonId, true)]
    public async Task ToggleAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        if (state.View is not TriggerView.Detail || state.SelectedTriggerId is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        var trigger = await db.Set<Trigger>().FindAsync(state.SelectedTriggerId.Value);
        if (trigger is null)
        {
            await state.ReloadAsync(db, Context.Guild);
            await RenderAsync(paginator);
            return;
        }

        await moderation.ToggleTriggerAsync(trigger, (IGuildUser)Context.User, state: null);
        cache.InvalidateCaches(Context.Guild);

        await state.ReloadAsync(db, Context.Guild);
        state.LastUpdated = DateTimeOffset.UtcNow;

        await RenderAsync(paginator);
    }

    [ComponentInteraction(DeleteButtonId, true)]
    public async Task DeleteAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        if (state.View is not TriggerView.Detail || state.SelectedTriggerId is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.View = TriggerView.ConfirmDelete;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(DeleteLoudButtonId, true)]
    public Task ConfirmDeleteLoudAsync() => ConfirmDeleteAsync(silent: false);

    [ComponentInteraction(DeleteSilentButtonId, true)]
    public Task ConfirmDeleteSilentAsync() => ConfirmDeleteAsync(silent: true);

    private async Task ConfirmDeleteAsync(bool silent)
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        if (state.View is not TriggerView.ConfirmDelete || state.SelectedTriggerId is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        var trigger = await db.Set<Trigger>().FindAsync(state.SelectedTriggerId.Value);
        if (trigger is null)
        {
            state.View = TriggerView.List;
            state.SelectedTriggerId = null;
            await state.ReloadAsync(db, Context.Guild);
            paginator.PageCount = state.GetPageCount();
            paginator.SetPage(0);
            await RenderAsync(paginator);
            return;
        }

        await moderation.DeleteTriggerAsync(trigger, (IGuildUser)Context.User, silent);
        cache.InvalidateCaches(Context.Guild);

        state.View = TriggerView.List;
        state.SelectedTriggerId = null;
        state.LastUpdated = DateTimeOffset.UtcNow;
        await state.ReloadAsync(db, Context.Guild);
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await RenderAsync(paginator);
    }

    private bool TryGetPanel(out IComponentPaginator paginator, out TriggerPanelState state)
    {
        if (Context.Interaction is not IComponentInteraction i
            || !interactive.TryGetComponentPaginator(i.Message, out var p)
            || p is null
            || !p.CanInteract(i.User))
        {
            paginator = null!;
            state = null!;
            return false;
        }

        paginator = p;
        state = paginator.GetUserState<TriggerPanelState>();
        return true;
    }

    private async Task RenderAsync(IComponentPaginator paginator)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(Context.Interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    private static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<TriggerPanelState>();
        var disabled = p.ShouldDisable();

        var filter = state.Filter.Kind switch
        {
            TriggerCategoryFilterKind.All => "All",
            TriggerCategoryFilterKind.GlobalOnly => "Global",
            TriggerCategoryFilterKind.Category => state.Categories.FirstOrDefault(c => c.Id == state.Filter.CategoryId)?.Name ?? "Category",
            _ => "All"
        };

        var container = new ContainerBuilder()
            .WithTextDisplay($"## Triggers\n**Guild:** {state.GuildName}\n**Filter:** {filter}")
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        switch (state.View)
        {
            case TriggerView.List:
                BuildList(container, state, p);
                break;
            case TriggerView.Detail:
                BuildDetail(container, state);
                break;
            case TriggerView.ConfirmDelete:
                BuildConfirmDelete(container, state);
                break;
        }

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        var categoryOptions = new List<SelectMenuOptionBuilder>
        {
            new("All Categories", TriggerCategoryFilter.All.Value, isDefault: state.Filter.Kind is TriggerCategoryFilterKind.All),
            new("Global Only", TriggerCategoryFilter.GlobalOnly.Value, isDefault: state.Filter.Kind is TriggerCategoryFilterKind.GlobalOnly)
        };

        foreach (var c in state.Categories.Take(23))
        {
            categoryOptions.Add(new SelectMenuOptionBuilder(
                c.Name.Truncate(100),
                c.Id.ToString(),
                isDefault: state.Filter.Kind is TriggerCategoryFilterKind.Category && state.Filter.CategoryId == c.Id));
        }

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(CategorySelectId)
                .WithPlaceholder("Filter by category…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(categoryOptions)
                .WithDisabled(disabled)));

        var nav = new ActionRowBuilder()
            .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
            .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
            .AddNextButton(p, "▶", ButtonStyle.Secondary);

        container.WithActionRow(nav);

        var controls = new ActionRowBuilder()
            .WithButton("Refresh", RefreshButtonId, ButtonStyle.Secondary, disabled: disabled);

        if (state.View is TriggerView.Detail or TriggerView.ConfirmDelete)
            controls.WithButton("Back", BackButtonId, ButtonStyle.Secondary, disabled: disabled);

        controls.AddStopButton(p, "Close", ButtonStyle.Danger);
        container.WithActionRow(controls);

        if (state.LastUpdated is not null)
        {
            container
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay($"-# Updated {state.LastUpdated.Value.ToUniversalTimestamp()} • {state.Triggers.Count} triggers")
                .WithAccentColor(AccentColor);
        }
        else
        {
            container.WithAccentColor(AccentColor);
        }

        return new PageBuilder()
            .WithComponents(new ComponentBuilderV2().WithContainer(container).Build())
            .Build();
    }

    private static void BuildList(ContainerBuilder container, TriggerPanelState state, IComponentPaginator p)
    {
        var pageItems = state.GetTriggersForPage(p.CurrentPageIndex).ToList();
        if (pageItems.Count == 0)
        {
            container.WithTextDisplay("-# No triggers match your filter.");
            return;
        }

        var list = string.Join("\n", pageItems.Select((t, i) =>
            $"**{i + 1}.** {t.Title} • {OnOff(t.IsActive)}\n-# `{t.Id}`"));

        container.WithTextDisplay(list.Truncate(3200));

        var options = pageItems.Select(t =>
            new SelectMenuOptionBuilder(
                $"{t.Title.Truncate(80)} • {OnOff(t.IsActive)}".Truncate(100),
                t.Id.ToString(),
                description: t.Summary.Truncate(100))).ToList();

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(TriggerSelectId)
                .WithPlaceholder("Select a trigger…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(options)
                .WithDisabled(p.ShouldDisable())));
    }

    private static void BuildDetail(ContainerBuilder container, TriggerPanelState state)
    {
        if (state.SelectedTriggerId is null)
        {
            container.WithTextDisplay("-# No trigger selected.");
            return;
        }

        var trigger = state.Triggers.FirstOrDefault(t => t.Id == state.SelectedTriggerId.Value);
        if (trigger is null)
        {
            container.WithTextDisplay("-# That trigger no longer exists.");
            return;
        }

        container.WithTextDisplay(string.Join("\n", new[]
        {
            $"### {trigger.Title}",
            $"**Status:** {OnOff(trigger.IsActive)}",
            $"**Category:** {trigger.CategoryName}",
            $"**Details:** {trigger.Details}",
            $"**Modified by:** {trigger.ModifiedBy}",
            $"-# `{trigger.Id}`"
        }).Truncate(3800));

        container.WithActionRow(new ActionRowBuilder()
            .WithButton(trigger.IsActive ? "Disable" : "Enable", ToggleButtonId, ButtonStyle.Secondary)
            .WithButton("Delete…", DeleteButtonId, ButtonStyle.Danger));
    }

    private static void BuildConfirmDelete(ContainerBuilder container, TriggerPanelState state)
    {
        if (state.SelectedTriggerId is null)
        {
            container.WithTextDisplay("-# No trigger selected.");
            return;
        }

        var trigger = state.Triggers.FirstOrDefault(t => t.Id == state.SelectedTriggerId.Value);
        if (trigger is null)
        {
            container.WithTextDisplay("-# That trigger no longer exists.");
            return;
        }

        container.WithTextDisplay(string.Join("\n", new[]
        {
            "### Confirm deletion",
            $"This will delete **{trigger.Title}** and **all associated reprimands**.",
            "",
            "- **Delete with notifications**: updates each reprimand with a deletion reason",
            "- **Delete silently**: deletes without notifying each reprimand (use if there are many)"
        }).Truncate(3800));

        container.WithActionRow(new ActionRowBuilder()
            .WithButton("Delete", DeleteLoudButtonId, ButtonStyle.Danger)
            .WithButton("Delete silently", DeleteSilentButtonId, ButtonStyle.Danger)
            .WithButton("Cancel", BackButtonId, ButtonStyle.Secondary));
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";

    private enum TriggerView
    {
        List,
        Detail,
        ConfirmDelete
    }

    private enum TriggerCategoryFilterKind
    {
        All,
        GlobalOnly,
        Category
    }

    private sealed record TriggerCategoryFilter(TriggerCategoryFilterKind Kind, Guid? CategoryId)
    {
        public static TriggerCategoryFilter All { get; } = new(TriggerCategoryFilterKind.All, null);
        public static TriggerCategoryFilter GlobalOnly { get; } = new(TriggerCategoryFilterKind.GlobalOnly, null);

        public string Value => Kind switch
        {
            TriggerCategoryFilterKind.All => "all",
            TriggerCategoryFilterKind.GlobalOnly => "global",
            TriggerCategoryFilterKind.Category => CategoryId?.ToString() ?? "all",
            _ => "all"
        };

        public string DisplayName => Kind switch
        {
            TriggerCategoryFilterKind.All => "All",
            TriggerCategoryFilterKind.GlobalOnly => "Global",
            TriggerCategoryFilterKind.Category => "Category",
            _ => "All"
        };

        public static TriggerCategoryFilter Parse(string value)
        {
            if (value == "all") return All;
            if (value == "global") return GlobalOnly;
            return Guid.TryParse(value, out var id) ? new TriggerCategoryFilter(TriggerCategoryFilterKind.Category, id) : All;
        }
    }

    private sealed class TriggerPanelState
    {
        private const int PageSize = 6;

        public required string GuildName { get; init; }
        public required IReadOnlyList<CategoryOption> Categories { get; init; }

        public TriggerCategoryFilter Filter { get; set; } = TriggerCategoryFilter.All;
        public TriggerView View { get; set; } = TriggerView.List;
        public Guid? SelectedTriggerId { get; set; }

        public List<TriggerEntry> Triggers { get; } = new();
        public DateTimeOffset? LastUpdated { get; set; }

        public static async Task<TriggerPanelState> CreateAsync(
            HuTaoContext db,
            IGuild guild,
            ModerationCategory? category)
        {
            var guildEntity = await db.Guilds.TrackGuildAsync(guild);
            var categories = guildEntity.ModerationCategories
                .Select(c => new CategoryOption(c.Id, c.Name))
                .OrderBy(c => c.Name)
                .ToList();

            var filter = category switch
            {
                null => TriggerCategoryFilter.All,
                { Name: var name } when name.Equals(nameof(ModerationCategory.All), StringComparison.OrdinalIgnoreCase)
                    => TriggerCategoryFilter.All,
                { Id: var id } when id == Guid.Empty => TriggerCategoryFilter.GlobalOnly,
                { Id: var id } => new TriggerCategoryFilter(TriggerCategoryFilterKind.Category, id)
            };

            var state = new TriggerPanelState
            {
                GuildName = guild.Name,
                Categories = categories,
                Filter = filter,
                LastUpdated = DateTimeOffset.UtcNow
            };

            await state.ReloadAsync(db, guild);
            return state;
        }

        public int GetPageCount()
        {
            return View switch
            {
                TriggerView.List => Math.Max(1, (int)Math.Ceiling((double)Triggers.Count / PageSize)),
                _ => 1
            };
        }

        public IEnumerable<TriggerEntry> GetTriggersForPage(int pageIndex)
            => Triggers.Skip(pageIndex * PageSize).Take(PageSize);

        public async Task ReloadAsync(HuTaoContext db, IGuild guild)
        {
            var guildEntity = await db.Guilds.TrackGuildAsync(guild);
            var rules = guildEntity.ModerationRules ??= new ModerationRules();

            var raw = rules.Triggers
                .OfType<ReprimandTrigger>()
                .ToList();

            var filtered = Filter.Kind switch
            {
                TriggerCategoryFilterKind.All => raw,
                TriggerCategoryFilterKind.GlobalOnly => raw.Where(t => t.Category is null).ToList(),
                TriggerCategoryFilterKind.Category => raw.Where(t => t.Category?.Id == Filter.CategoryId).ToList(),
                _ => raw
            };

            Triggers.Clear();
            foreach (var t in filtered.OrderByDescending(x => x.Action?.Date))
                Triggers.Add(TriggerEntry.From(t));
        }
    }

    private sealed record CategoryOption(Guid Id, string Name);

    private sealed record TriggerEntry(
        Guid Id,
        string Title,
        string Summary,
        string Details,
        string CategoryName,
        bool IsActive,
        string ModifiedBy)
    {
        public static TriggerEntry From(ReprimandTrigger trigger)
        {
            var actionTitle = trigger.Reprimand?.GetTitle() ?? "Reprimand";
            var title = $"{actionTitle} • {trigger.Source.Humanize()}";
            var category = trigger.Category?.Name ?? "Global";
            var details = trigger.GetDetails();
            var summary = $"{details} • {category}";

            var modId = trigger.Action?.UserId;
            var modifiedBy = modId is null ? "Unknown" : $"`{MentionUtils.MentionUser(modId.Value)}` ({modId.Value})";

            return new TriggerEntry(
                trigger.Id,
                title,
                summary,
                details,
                category,
                trigger.IsActive,
                modifiedBy);
        }
    }
}

