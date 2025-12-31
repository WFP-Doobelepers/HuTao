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
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Logging;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.Logging;

[Group("log", "Logging configuration")]
[RequireContext(ContextType.Guild)]
public sealed class InteractiveLoggingExclusionsModule(
    HuTaoContext db,
    IMemoryCache cache,
    InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    private const string RemoveSelectId = "logex:remove";
    private const string AddUserSelectId = "logex:add-user";
    private const string AddRoleSelectId = "logex:add-role";
    private const string AddChannelSelectId = "logex:add-channel";
    private const string RefreshButtonId = "logex:refresh";

    [SlashCommand("exclusions", "Manage logging exclusions interactively.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task ExclusionsAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);

        var state = await LoggingExclusionsState.CreateAsync(db, Context.Guild);
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
    }

    [ComponentInteraction(RemoveSelectId, true)]
    public async Task RemoveAsync(string criterionId)
    {
        if (!TryGetPaginator(out var paginator, out var state, out var interaction))
            return;

        if (!Guid.TryParse(criterionId, out var id))
        {
            await DeferAsync();
            await RenderAsync(paginator, interaction);
            return;
        }

        await DeferAsync();

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.LoggingRules ??= new LoggingRules();

        var criteria = guild.LoggingRules.LoggingExclusions;
        var entity = criteria.FirstOrDefault(c => c.Id == id) ?? await db.Set<Criterion>().FindAsync(id);

        if (entity is not null)
        {
            criteria.Remove(entity);
            db.Remove(entity);
            cache.InvalidateCaches(Context.Guild);
            await db.SaveChangesAsync();
        }

        await state.ReloadAsync(db, Context.Guild);
        paginator.PageCount = state.GetPageCount();
        if (paginator.CurrentPageIndex >= paginator.PageCount)
            paginator.SetPage(Math.Max(0, paginator.PageCount - 1));

        await RenderAsync(paginator, interaction);
    }

    [ComponentInteraction(AddUserSelectId, true)]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddUserAsync(IUser[] users)
    {
        if (!TryGetPaginator(out var paginator, out var state, out var interaction))
            return;

        var user = users.FirstOrDefault();
        if (user is null)
        {
            await DeferAsync();
            await RenderAsync(paginator, interaction);
            return;
        }

        await DeferAsync();

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.LoggingRules ??= new LoggingRules();

        if (guild.LoggingRules.LoggingExclusions.OfType<UserCriterion>().Any(c => c.UserId == user.Id))
        {
            await RenderAsync(paginator, interaction);
            return;
        }

        guild.LoggingRules.LoggingExclusions.Add(new UserCriterion(user.Id));
        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        await state.ReloadAsync(db, Context.Guild);
        paginator.PageCount = state.GetPageCount();

        await RenderAsync(paginator, interaction);
    }

    [ComponentInteraction(AddRoleSelectId, true)]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddRoleAsync(IRole[] roles)
    {
        if (!TryGetPaginator(out var paginator, out var state, out var interaction))
            return;

        var role = roles.FirstOrDefault();
        if (role is null)
        {
            await DeferAsync();
            await RenderAsync(paginator, interaction);
            return;
        }

        await DeferAsync();

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.LoggingRules ??= new LoggingRules();

        if (guild.LoggingRules.LoggingExclusions.OfType<RoleCriterion>().Any(c => c.RoleId == role.Id))
        {
            await RenderAsync(paginator, interaction);
            return;
        }

        guild.LoggingRules.LoggingExclusions.Add(new RoleCriterion(role));
        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        await state.ReloadAsync(db, Context.Guild);
        paginator.PageCount = state.GetPageCount();

        await RenderAsync(paginator, interaction);
    }

    [ComponentInteraction(AddChannelSelectId, true)]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddChannelAsync(IGuildChannel[] channels)
    {
        if (!TryGetPaginator(out var paginator, out var state, out var interaction))
            return;

        var channel = channels.FirstOrDefault();
        if (channel is null)
        {
            await DeferAsync();
            await RenderAsync(paginator, interaction);
            return;
        }

        if (channel is not (ITextChannel or ICategoryChannel))
        {
            await DeferAsync();
            await RenderAsync(paginator, interaction);
            return;
        }

        await DeferAsync();

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.LoggingRules ??= new LoggingRules();

        if (guild.LoggingRules.LoggingExclusions.OfType<ChannelCriterion>().Any(c => c.ChannelId == channel.Id))
        {
            await RenderAsync(paginator, interaction);
            return;
        }

        guild.LoggingRules.LoggingExclusions.Add(new ChannelCriterion(channel.Id, channel is ICategoryChannel));
        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        await state.ReloadAsync(db, Context.Guild);
        paginator.PageCount = state.GetPageCount();

        await RenderAsync(paginator, interaction);
    }

    [ComponentInteraction(RefreshButtonId, true)]
    public async Task RefreshAsync()
    {
        if (!TryGetPaginator(out var paginator, out var state, out var interaction))
            return;

        await DeferAsync();

        await state.ReloadAsync(db, Context.Guild);
        paginator.PageCount = state.GetPageCount();
        if (paginator.CurrentPageIndex >= paginator.PageCount)
            paginator.SetPage(Math.Max(0, paginator.PageCount - 1));

        await RenderAsync(paginator, interaction);
    }

    private bool TryGetPaginator(out IComponentPaginator paginator, out LoggingExclusionsState state, out IComponentInteraction interaction)
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
        state = paginator.GetUserState<LoggingExclusionsState>();
        return true;
    }

    private static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<LoggingExclusionsState>();
        var disabled = p.ShouldDisable();

        var container = new ContainerBuilder()
            .WithTextDisplay($"## Logging Exclusions\n**Total:** {state.Entries.Count}\n-# These rules skip logging when matched")
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        var page = state.GetPage(p.CurrentPageIndex);
        if (page.Count == 0)
        {
            container.WithTextDisplay("-# No exclusions yet. Add some below.");
        }
        else
        {
            var list = string.Join("\n", page.Select((e, i) =>
                $"**{i + 1}.** {e.Kind}\n{e.Display}\n-# `{e.Id}`"));
            container.WithTextDisplay(list.Truncate(3200));
        }

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        var removeOptions = page
            .Select(e => new SelectMenuOptionBuilder(
                $"{e.Kind}: {e.ShortLabel}".Truncate(100),
                e.Id.ToString()))
            .ToList();

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(RemoveSelectId)
                .WithPlaceholder(removeOptions.Count == 0 ? "No exclusions to remove" : "Remove an exclusion…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(removeOptions)
                .WithDisabled(disabled || removeOptions.Count == 0)));

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(AddUserSelectId)
                .WithPlaceholder("Add excluded user…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithType(ComponentType.UserSelect)
                .WithDisabled(disabled)));

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(AddRoleSelectId)
                .WithPlaceholder("Add excluded role…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithType(ComponentType.RoleSelect)
                .WithDisabled(disabled)));

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(AddChannelSelectId)
                .WithPlaceholder("Add excluded channel/category…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithType(ComponentType.ChannelSelect)
                .WithChannelTypes(new[] { ChannelType.Text, ChannelType.News, ChannelType.Category })
                .WithDisabled(disabled)));

        container.WithActionRow(new ActionRowBuilder()
            .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
            .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
            .AddNextButton(p, "▶", ButtonStyle.Secondary)
            .WithButton("Refresh", RefreshButtonId, ButtonStyle.Secondary, disabled: disabled)
            .AddStopButton(p, "Close", ButtonStyle.Danger));

        container.WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small);
        container.WithTextDisplay($"-# Page {p.CurrentPageIndex + 1} of {p.PageCount}")
            .WithAccentColor(AccentColor);

        return new PageBuilder()
            .WithComponents(new ComponentBuilderV2().WithContainer(container).Build())
            .Build();
    }

    private async Task RenderAsync(IComponentPaginator paginator, IComponentInteraction interaction)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    private sealed class LoggingExclusionsState
    {
        private const int PageSize = 8;

        public List<CriterionEntry> Entries { get; } = new();

        public static async Task<LoggingExclusionsState> CreateAsync(HuTaoContext db, IGuild guild)
        {
            var state = new LoggingExclusionsState();
            await state.ReloadAsync(db, guild);
            return state;
        }

        public int GetPageCount()
            => Math.Max(1, (int)Math.Ceiling((double)Entries.Count / PageSize));

        public IReadOnlyList<CriterionEntry> GetPage(int pageIndex)
        {
            return Entries
                .Skip(pageIndex * PageSize)
                .Take(PageSize)
                .ToList();
        }

        public async Task ReloadAsync(HuTaoContext db, IGuild guild)
        {
            var guildEntity = await db.Guilds.TrackGuildAsync(guild);
            var rules = guildEntity.LoggingRules;

            var criteria = rules?.LoggingExclusions
                .OrderBy(c => c.GetType().Name)
                .ThenBy(c => c.ToString(), StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<Criterion>();

            Entries.Clear();
            foreach (var c in criteria)
                Entries.Add(CriterionEntry.From(c));
        }
    }

    private sealed record CriterionEntry(Guid Id, string Kind, string Display, string ShortLabel)
    {
        public static CriterionEntry From(Criterion criterion)
        {
            var (kind, display) = criterion switch
            {
                UserCriterion u => ("User", $"`{MentionUtils.MentionUser(u.UserId)}`"),
                RoleCriterion r => ("Role", $"`{MentionUtils.MentionRole(r.RoleId)}`"),
                ChannelCriterion c => (c.IsCategory ? "Category" : "Channel", $"`{MentionUtils.MentionChannel(c.ChannelId)}`"),
                PermissionCriterion p => ("Permission", $"`{p.Permission.Humanize()}`"),
                _ => ("Criterion", $"`{criterion}`")
            };

            return new CriterionEntry(criterion.Id, kind, display, $"{kind}");
        }
    }
}

