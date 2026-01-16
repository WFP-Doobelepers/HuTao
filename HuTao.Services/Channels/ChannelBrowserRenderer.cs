using System;
using System.Linq;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Services.Utilities;

namespace HuTao.Services.Channels;

public static class ChannelBrowserRenderer
{
    private const uint AccentColor = 0x9B59FF;

    public static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<ChannelBrowserState>();
        var disabled = p.ShouldDisable();

        var filterText = state.Filter.ToString();
        var searchText = string.IsNullOrWhiteSpace(state.Search) ? "None" : state.Search;

        var container = new ContainerBuilder()
            .WithTextDisplay($"## Channels\n**Guild:** {state.GuildName}\n**Filter:** {filterText}\n**Search:** {searchText}")
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        if (!string.IsNullOrWhiteSpace(state.Notice))
        {
            container.WithTextDisplay($"-# {state.Notice.Truncate(600)}");
            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        switch (state.View)
        {
            case ChannelBrowserView.List:
                AddList(container, state, p);
                break;
            case ChannelBrowserView.Detail:
                AddDetail(container, state);
                break;
        }

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        if (state.View is ChannelBrowserView.List)
        {
            var filterOptions = Enum.GetValues<ChannelBrowserFilter>()
                .Select(f => new SelectMenuOptionBuilder(f.ToString(), f.ToString(), isDefault: f == state.Filter))
                .ToList();

            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(ChannelBrowserComponentIds.FilterSelectId)
                    .WithPlaceholder("Filter…")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithOptions(filterOptions)
                    .WithDisabled(disabled)));

            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(ChannelBrowserComponentIds.ChannelSelectId)
                    .WithPlaceholder("Jump to a channel…")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithType(ComponentType.ChannelSelect)
                    .WithChannelTypes(new[] { ChannelType.Text, ChannelType.News, ChannelType.Voice, ChannelType.Stage, ChannelType.Category })
                    .WithDisabled(disabled)));

            container.WithActionRow(new ActionRowBuilder()
                .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
                .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
                .AddNextButton(p, "▶", ButtonStyle.Secondary));

            container.WithActionRow(new ActionRowBuilder()
                .WithButton("Search", ChannelBrowserComponentIds.SearchButtonId, ButtonStyle.Primary, disabled: disabled)
                .WithButton("Clear", ChannelBrowserComponentIds.ClearButtonId, ButtonStyle.Secondary,
                    disabled: disabled || string.IsNullOrWhiteSpace(state.Search))
                .WithButton("Refresh", ChannelBrowserComponentIds.RefreshButtonId, ButtonStyle.Secondary, disabled: disabled)
                .AddStopButton(p, "Close", ButtonStyle.Danger));
        }
        else
        {
            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(ChannelBrowserComponentIds.ChannelSelectId)
                    .WithPlaceholder("Switch channel…")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithType(ComponentType.ChannelSelect)
                    .WithChannelTypes(new[] { ChannelType.Text, ChannelType.News, ChannelType.Voice, ChannelType.Stage, ChannelType.Category })
                    .WithDisabled(disabled)));

            container.WithActionRow(new ActionRowBuilder()
                .WithButton("Back", ChannelBrowserComponentIds.BackButtonId, ButtonStyle.Secondary, disabled: disabled)
                .WithButton("Refresh", ChannelBrowserComponentIds.RefreshButtonId, ButtonStyle.Secondary, disabled: disabled)
                .AddStopButton(p, "Close", ButtonStyle.Danger));
        }

        if (state.LastUpdated is not null)
        {
            container
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay($"-# Updated {state.LastUpdated.Value.ToUniversalTimestamp()}")
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

    private static void AddList(ContainerBuilder container, ChannelBrowserState state, IComponentPaginator p)
    {
        var page = state.GetPage(p.CurrentPageIndex);
        if (page.Count == 0)
        {
            container.WithTextDisplay("-# No channels match your filter.");
            return;
        }

        var list = string.Join("\n", page.Select((c, i) =>
        {
            var cat = c.CategoryId is null ? "No category" : MentionUtils.MentionChannel(c.CategoryId.Value);
            return $"**{i + 1}.** {c.Mention} — `{c.Id}`\n-# {c.Kind} • {cat} • Pos {c.Position}";
        }));

        container.WithTextDisplay(list.Truncate(3200));
    }

    private static void AddDetail(ContainerBuilder container, ChannelBrowserState state)
    {
        var c = state.GetSelected();
        if (c is null)
        {
            container.WithTextDisplay("-# That channel no longer exists.");
            return;
        }

        var cat = c.CategoryId is null ? "None" : MentionUtils.MentionChannel(c.CategoryId.Value);
        var slowmode = c.SlowmodeSeconds is null or 0 ? "Off" : $"{c.SlowmodeSeconds}s";
        var limit = c.UserLimit is null or 0 ? "None" : c.UserLimit.Value.ToString();
        var topic = string.IsNullOrWhiteSpace(c.Topic) ? "None" : c.Topic.Truncate(400);

        var lines = string.Join("\n", new[]
        {
            $"### {c.Mention} ({c.Id})",
            $"- Name: {c.Name}",
            $"- Type: {c.Kind}",
            $"- Category: {cat}",
            $"- Position: {c.Position}",
            $"- NSFW: {OnOff(c.IsNsfw)}",
            $"- Slowmode: {slowmode}",
            $"- User limit: {limit}",
            $"- Topic: {topic}"
        });

        container.WithTextDisplay(lines.Truncate(3800));
    }

    private static string OnOff(bool value) => value ? "On" : "Off";
}

