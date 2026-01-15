using System;
using System.Linq;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Services.Utilities;

namespace HuTao.Services.Roles;

public static class RoleBrowserRenderer
{
    private const uint AccentColor = 0x9B59FF;

    public static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<RoleBrowserState>();
        var disabled = p.ShouldDisable();

        var filteredCount = state.GetFilteredRoles().Count;
        var filterLabel = string.IsNullOrWhiteSpace(state.Filter) ? "All" : state.Filter;

        var container = new ContainerBuilder()
            .WithTextDisplay($"## Roles\n**Guild:** {state.GuildName}\n**Filter:** {filterLabel}\n**Total:** {filteredCount} / {state.Roles.Count}")
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        switch (state.View)
        {
            case RoleBrowserView.List:
                AddRoleList(container, state, p);
                break;
            case RoleBrowserView.Detail:
                AddRoleDetail(container, state);
                break;
        }

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        if (state.View is RoleBrowserView.List)
        {
            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(RoleBrowserComponentIds.RoleSelectId)
                    .WithPlaceholder("Jump to a role…")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithType(ComponentType.RoleSelect)
                    .WithDisabled(disabled)));

            container.WithActionRow(new ActionRowBuilder()
                .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
                .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
                .AddNextButton(p, "▶", ButtonStyle.Secondary));

            container.WithActionRow(new ActionRowBuilder()
                .WithButton("Search", RoleBrowserComponentIds.SearchButtonId, ButtonStyle.Primary, disabled: disabled)
                .WithButton("Clear", RoleBrowserComponentIds.ClearButtonId, ButtonStyle.Secondary,
                    disabled: disabled || string.IsNullOrWhiteSpace(state.Filter))
                .WithButton("Refresh", RoleBrowserComponentIds.RefreshButtonId, ButtonStyle.Secondary, disabled: disabled)
                .AddStopButton(p, "Close", ButtonStyle.Danger));
        }
        else
        {
            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(RoleBrowserComponentIds.RoleSelectId)
                    .WithPlaceholder("Switch role…")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithType(ComponentType.RoleSelect)
                    .WithDisabled(disabled)));

            container.WithActionRow(new ActionRowBuilder()
                .WithButton("Back", RoleBrowserComponentIds.BackButtonId, ButtonStyle.Secondary, disabled: disabled)
                .WithButton("Refresh", RoleBrowserComponentIds.RefreshButtonId, ButtonStyle.Secondary, disabled: disabled)
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

    private static void AddRoleList(ContainerBuilder container, RoleBrowserState state, IComponentPaginator p)
    {
        var page = state.GetPage(p.CurrentPageIndex);
        if (page.Count == 0)
        {
            container.WithTextDisplay("-# No roles match your filter.");
            return;
        }

        var list = string.Join("\n", page.Select((r, i) =>
            $"**{i + 1}.** {r.Mention} — `{r.Id}`\n-# Members: {r.MemberCount} • Pos: {r.Position}"));

        container.WithTextDisplay(list.Truncate(3200));
    }

    private static void AddRoleDetail(ContainerBuilder container, RoleBrowserState state)
    {
        var role = state.GetSelectedRole();
        if (role is null)
        {
            container.WithTextDisplay("-# That role no longer exists.");
            return;
        }

        var color = role.Color == 0 ? "Default" : $"#{role.Color:X6}";

        var lines = string.Join("\n", new[]
        {
            $"### {role.Mention} ({role.Id})",
            $"- Name: {role.Name}",
            $"- Color: {color}",
            $"- Position: {role.Position}",
            $"- Members: {role.MemberCount}",
            $"- Hoisted: {OnOff(role.IsHoisted)}",
            $"- Mentionable: {OnOff(role.IsMentionable)}",
            $"- Managed: {OnOff(role.IsManaged)}",
            "",
            "### Permissions",
            role.PermissionsText
        });

        container.WithTextDisplay(lines.Truncate(3800));
    }

    private static string OnOff(bool value) => value ? "On" : "Off";
}

