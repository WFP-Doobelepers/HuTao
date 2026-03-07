using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Bot.Modules.Moderation;

/// <summary>
/// Handles component interactions for moderation paginators using Components V2
/// </summary>
public class ModerationInteractionHandlers : InteractionModuleBase<SocketInteractionContext>
{
    public HuTaoContext Db { get; init; } = null!;
    public InteractiveService Interactive { get; init; } = null!;
    public ModerationService ModerationService { get; init; } = null!;
    public AuthorizationService Auth { get; init; } = null!;

    [ComponentInteraction("mute-action:unmute:*")]
    public async Task HandleMuteUnmuteAsync(string muteIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("You cannot interact with this paginator.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(muteIdString, out var muteId))
            {
                await FollowupAsync("Invalid mute ID.", ephemeral: true);
                return;
            }

            var mute = await Db.Set<Mute>().FirstOrDefaultAsync(m => m.Id == muteId);
            if (mute is null)
            {
                await FollowupAsync("Mute not found or already removed.", ephemeral: true);
                return;
            }

            var hasPermission = await Auth.IsAuthorizedAsync(Context, AuthorizationScope.Mute);
            if (!hasPermission)
            {
                await FollowupAsync("You don't have permission to unmute users.", ephemeral: true);
                return;
            }

            var user = await Context.Client.Rest.GetUserAsync(mute.UserId);
            var details = new ReprimandDetails(user, (IGuildUser)Context.User, "Manual unmute via paginator");

            var result = await ModerationService.TryUnmuteAsync(details);
            if (result)
            {
                var state = paginator.GetUserState<MuteListPaginatorState>();
                var refreshedMutes = await RefreshMuteData(state.Category);
                state.UpdateData(refreshedMutes, state.Category);
                paginator.PageCount = state.TotalPages;

                await paginator.RenderPageAsync(interaction, InteractionResponseType.DeferredUpdateMessage, false);
                await FollowupAsync($"**<@{user.Id}>** has been unmuted successfully.", ephemeral: true);
            }
            else
            {
                await FollowupAsync("Failed to unmute user. They may not be muted or an error occurred.", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            await FollowupAsync($"An error occurred: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("mute-action:extend:*")]
    public async Task HandleMuteExtendAsync(string muteIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("You cannot interact with this paginator.", ephemeral: true);
            return;
        }

        if (!Guid.TryParse(muteIdString, out _))
        {
            await RespondAsync("Invalid mute ID.", ephemeral: true);
            return;
        }

        await RespondWithModalAsync<MuteExtendModal>($"mute-extend-modal:{muteIdString}");
    }

    [ModalInteraction("mute-extend-modal:*")]
    public async Task HandleMuteExtendModalAsync(string muteIdString, MuteExtendModal modal)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(muteIdString, out var muteId))
        {
            await FollowupAsync("Invalid mute ID.", ephemeral: true);
            return;
        }

        var mute = await Db.Set<Mute>()
            .Include(m => m.Action)
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == muteId);

        if (mute is null)
        {
            await FollowupAsync("Mute not found.", ephemeral: true);
            return;
        }

        var hasPermission = await Auth.IsAuthorizedAsync(Context, AuthorizationScope.Mute);
        if (!hasPermission)
        {
            await FollowupAsync("You don't have permission to extend mutes.", ephemeral: true);
            return;
        }

        if (modal.Duration is null)
        {
            await FollowupAsync("Please provide a valid duration.", ephemeral: true);
            return;
        }

        var user = await Context.Client.Rest.GetUserAsync(mute.UserId);
        if (user is null)
        {
            await FollowupAsync("User not found.", ephemeral: true);
            return;
        }

        var reason = modal.Reason ?? $"Mute extended by {modal.Duration.Value.Humanize()}";
        var details = new ReprimandDetails(user, (IGuildUser)Context.User, reason, Category: mute.Category);

        await ModerationService.TryUnmuteAsync(details);
        var result = await ModerationService.TryMuteAsync(modal.Duration, details);

        if (result is not null)
            await FollowupAsync($"Mute for <@{mute.UserId}> extended by **{modal.Duration.Value.Humanize()}**.", ephemeral: true);
        else
            await FollowupAsync("Failed to extend mute. The user may no longer be muted.", ephemeral: true);
    }

    [ComponentInteraction("mute-action:details:*")]
    public async Task HandleMuteDetailsAsync(string muteIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("You cannot interact with this paginator.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(muteIdString, out var muteId))
            {
                await FollowupAsync("Invalid mute ID.", ephemeral: true);
                return;
            }

            var mute = await Db.Set<Mute>()
                .Include(m => m.Action)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == muteId);

            if (mute is null)
            {
                await FollowupAsync("Mute not found.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Mute Details - {mute.UserId}")
                .WithColor(Color.Orange)
                .AddField("User ID", mute.UserId, true)
                .AddField("Status", mute.Status.ToString(), true)
                .AddField("Duration", mute.Length?.Humanize() ?? "Permanent", true)
                .AddField("Reason", mute.Action?.Reason ?? "No reason provided")
                .AddField("Moderator", mute.Action?.Moderator is { } mod ? mod.Id.ToString() : "System", true)
                .AddField("Date", mute.Action?.Date.ToString("MMM dd, yyyy HH:mm") ?? "Unknown", true)
                .WithFooter($"Mute ID: {mute.Id}")
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (mute.Category is not null)
                embed.AddField("Category", mute.Category.Name, true);

            await FollowupAsync(
                components: embed.Build().ToComponentsV2Message(),
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"An error occurred: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("mute-category-filter")]
    public async Task HandleMuteCategoryFilterAsync(string categoryValue)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await DeferAsync();
            return;
        }

        await DeferAsync();

        var state = paginator.GetUserState<MuteListPaginatorState>();

        ModerationCategory? newCategory = null;
        if (categoryValue != "all" && Guid.TryParse(categoryValue, out var categoryGuid))
        {
            newCategory = state.Guild.ModerationCategories.FirstOrDefault(c => c.Id == categoryGuid);
        }

        // Refresh data with new filter
        var filteredMutes = await RefreshMuteData(newCategory);
        state.UpdateData(filteredMutes, newCategory);
        paginator.PageCount = state.TotalPages;
        paginator.SetPage(0); // Reset to first page when filtering

        await paginator.RenderPageAsync(interaction, InteractionResponseType.DeferredUpdateMessage, false);
    }

    [ComponentInteraction("mute-refresh")]
    public async Task HandleMuteRefreshAsync()
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await DeferAsync();
            return;
        }

        await DeferAsync();

        var state = paginator.GetUserState<MuteListPaginatorState>();
        var refreshedMutes = await RefreshMuteData(state.Category);
        state.UpdateData(refreshedMutes, state.Category);
        paginator.PageCount = state.TotalPages;

        await paginator.RenderPageAsync(interaction, InteractionResponseType.DeferredUpdateMessage, false);
    }

    private async Task<IReadOnlyList<Mute>> RefreshMuteData(ModerationCategory? category)
    {
        var guild = await Db.Guilds.Include(g => g.ReprimandHistory).FirstAsync(g => g.Id == Context.Guild.Id);

        return guild.ReprimandHistory.OfType<Mute>()
            .Where(r => r.IsActive())
            .Where(r => r.Status
                is not ReprimandStatus.Expired
            and not ReprimandStatus.Pardoned
            and not ReprimandStatus.Deleted)
            .Where(r => category is null || r.Category?.Id == category.Id)
            .OrderByDescending(r => r.Action?.Date)
            .ToList();
    }

    [ComponentInteraction("auto-toggle:*")]
    public async Task HandleAutoToggleAsync(string triggerId)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(triggerId, out var id))
        {
            await FollowupAsync("Invalid trigger ID.", ephemeral: true);
            return;
        }

        var trigger = await Db.Set<AutoConfiguration>().FindAsync(id);
        if (trigger is null)
        {
            await FollowupAsync("Trigger not found.", ephemeral: true);
            return;
        }

        var hasPermission = await Auth.IsAuthorizedAsync(Context, AuthorizationScope.Configuration);
        if (!hasPermission)
        {
            await FollowupAsync("You don't have permission to modify triggers.", ephemeral: true);
            return;
        }

        await ModerationService.ToggleTriggerAsync(trigger, (IGuildUser)Context.User, state: null);
        await FollowupAsync(
            $"Trigger `{trigger.Id}` is now **{(trigger.IsActive ? "enabled" : "disabled")}**.",
            ephemeral: true);
    }

    [ComponentInteraction("auto-delete:*")]
    public async Task HandleAutoDeleteAsync(string triggerId)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(triggerId, out var id))
        {
            await FollowupAsync("Invalid trigger ID.", ephemeral: true);
            return;
        }

        var trigger = await Db.Set<AutoConfiguration>().FindAsync(id);
        if (trigger is null)
        {
            await FollowupAsync("Trigger not found or already deleted.", ephemeral: true);
            return;
        }

        var hasPermission = await Auth.IsAuthorizedAsync(Context, AuthorizationScope.Configuration);
        if (!hasPermission)
        {
            await FollowupAsync("You don't have permission to delete triggers.", ephemeral: true);
            return;
        }

        await ModerationService.DeleteTriggerAsync(trigger, (IGuildUser)Context.User, silent: true);
        await FollowupAsync($"Trigger `{id}` has been deleted.", ephemeral: true);
    }

    // User History V2 Interaction Handlers
    [ComponentInteraction("history-toggle-images")]
    public async Task HandleHistoryToggleImagesAsync()
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await DeferAsync();
            return;
        }

        await DeferAsync();

        var state = paginator.GetUserState<UserHistoryPaginatorState>();
        state.ShowImages = !state.ShowImages;

        await paginator.RenderPageAsync(interaction, InteractionResponseType.DeferredUpdateMessage, false);
    }

    [ComponentInteraction("history-type-filter")]
    public async Task HandleHistoryTypeFilterAsync(string typeFilter)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await DeferAsync();
            return;
        }

        await DeferAsync();

        var state = paginator.GetUserState<UserHistoryPaginatorState>();
        var newType = typeFilter switch
        {
            "warnings" => LogReprimandType.Warning,
            "mutes" => LogReprimandType.Mute,
            "bans" => LogReprimandType.Ban,
            _ => LogReprimandType.None
        };

        state.UpdateFilters(state.CategoryFilter, newType);
        paginator.PageCount = state.TotalPages;
        paginator.SetPage(0);

        await paginator.RenderPageAsync(interaction, InteractionResponseType.DeferredUpdateMessage, false);
    }

    [ComponentInteraction("history-category-filter")]
    public async Task HandleHistoryCategoryFilterAsync(string categoryValue)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await DeferAsync();
            return;
        }

        await DeferAsync();

        var state = paginator.GetUserState<UserHistoryPaginatorState>();

        ModerationCategory? newCategory = null;
        if (categoryValue != "all" && Guid.TryParse(categoryValue, out var categoryGuid))
        {
            newCategory = state.Guild.ModerationCategories.FirstOrDefault(c => c.Id == categoryGuid);
        }

        state.UpdateFilters(newCategory, state.TypeFilter);
        paginator.PageCount = state.TotalPages;
        paginator.SetPage(0);

        await paginator.RenderPageAsync(interaction, InteractionResponseType.DeferredUpdateMessage, false);
    }

}

public class MuteExtendModal : IModal
{
    public string Title => "Extend Mute";

    [RequiredInput]
    [InputLabel("Duration")]
    [ModalTextInput("duration", TextInputStyle.Short, "Example: 1h30m")]
    public TimeSpan? Duration { get; set; }

    [RequiredInput(false)]
    [InputLabel("Reason")]
    [ModalTextInput("reason", TextInputStyle.Paragraph, "Reason for extension...")]
    public string? Reason { get; set; }
}
