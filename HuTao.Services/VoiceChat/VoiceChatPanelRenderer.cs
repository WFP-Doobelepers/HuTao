using System;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Services.Utilities;

namespace HuTao.Services.VoiceChat;

public static class VoiceChatPanelRenderer
{
    private const uint AccentColor = 0x9B59FF;

    public static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<VoiceChatPanelState>();
        var disabled = p.ShouldDisable();

        var limitText = state.UserLimit is null or 0 ? "None" : state.UserLimit.Value.ToString();

        var container = new ContainerBuilder()
            .WithTextDisplay(string.Join("\n", new[]
            {
                "## Voice Chat Panel",
                $"**Guild:** {state.GuildName}",
                $"**Voice:** {state.VoiceChannelMention}",
                $"**Text:** {state.TextChannelMention}",
                $"**Owner:** {state.OwnerMention}",
                $"**Locked:** {OnOff(state.IsLocked)}",
                $"**Hidden:** {OnOff(state.IsHidden)}",
                $"**User limit:** {limitText}"
            }).Truncate(1800))
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        if (!string.IsNullOrWhiteSpace(state.Notice))
        {
            container.WithTextDisplay($"-# {state.Notice.Truncate(600)}");
            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        var lockLabel = state.IsLocked ? "Unlock" : "Lock";
        var hideLabel = state.IsHidden ? "Reveal" : "Hide";

        container.WithActionRow(new ActionRowBuilder()
            .WithButton(lockLabel, VoiceChatPanelComponentIds.LockButtonId, ButtonStyle.Secondary, disabled: disabled)
            .WithButton(hideLabel, VoiceChatPanelComponentIds.HideButtonId, ButtonStyle.Secondary, disabled: disabled)
            .WithButton("Set limit", VoiceChatPanelComponentIds.LimitButtonId, ButtonStyle.Primary, disabled: disabled)
            .WithButton("Refresh", VoiceChatPanelComponentIds.RefreshButtonId, ButtonStyle.Secondary, disabled: disabled)
            .AddStopButton(p, "Close", ButtonStyle.Danger));

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(VoiceChatPanelComponentIds.TransferSelectId)
                .WithPlaceholder("Transfer ownership to…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithType(ComponentType.UserSelect)
                .WithDisabled(disabled)));

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(VoiceChatPanelComponentIds.BanSelectId)
                .WithPlaceholder("Ban user…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithType(ComponentType.UserSelect)
                .WithDisabled(disabled)));

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(VoiceChatPanelComponentIds.UnbanSelectId)
                .WithPlaceholder("Unban user…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithType(ComponentType.UserSelect)
                .WithDisabled(disabled)));

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

    private static string OnOff(bool value) => value ? "On" : "Off";
}

