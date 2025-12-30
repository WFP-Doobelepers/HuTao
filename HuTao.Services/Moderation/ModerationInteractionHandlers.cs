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
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.Moderation;

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
            await RespondAsync("❌ You cannot interact with this paginator.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(muteIdString, out var muteId))
            {
                await FollowupAsync("❌ Invalid mute ID.", ephemeral: true);
                return;
            }

            var mute = await Db.Set<Mute>().FirstOrDefaultAsync(m => m.Id == muteId);
            if (mute == null)
            {
                await FollowupAsync("❌ Mute not found or already removed.", ephemeral: true);
                return;
            }

            // Check permissions
            var hasPermission = await Auth.IsAuthorizedAsync(Context, AuthorizationScope.Mute);
            if (!hasPermission)
            {
                await FollowupAsync("❌ You don't have permission to unmute users.", ephemeral: true);
                return;
            }

            // Perform unmute
            var user = await Context.Client.Rest.GetUserAsync(mute.UserId);
            var details = new ReprimandDetails(user, (IGuildUser)Context.User, "Manual unmute via paginator");

            var result = await ModerationService.TryUnmuteAsync(details);
            if (result)
            {
                // Refresh paginator data
                var state = paginator.GetUserState<MuteListPaginatorState>();
                var refreshedMutes = await RefreshMuteData(state.Category);
                state.UpdateData(refreshedMutes, state.Category);
                paginator.PageCount = state.TotalPages;

                await paginator.RenderPageAsync(interaction);
                await FollowupAsync($"✅ **<@{user.Id}>** has been unmuted successfully.", ephemeral: true);
            }
            else
            {
                await FollowupAsync("❌ Failed to unmute user. They may not be muted or an error occurred.", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            await FollowupAsync($"❌ An error occurred: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("mute-action:extend:*")]
    public async Task HandleMuteExtendAsync(string muteIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("❌ You cannot interact with this paginator.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(muteIdString, out var muteId))
            {
                await FollowupAsync("❌ Invalid mute ID.", ephemeral: true);
                return;
            }

            var mute = await Db.Set<Mute>().FirstOrDefaultAsync(m => m.Id == muteId);
            if (mute == null)
            {
                await FollowupAsync("❌ Mute not found.", ephemeral: true);
                return;
            }

            // Check permissions
            var hasPermission = await Auth.IsAuthorizedAsync(Context, AuthorizationScope.Mute);
            if (!hasPermission)
            {
                await FollowupAsync("❌ You don't have permission to extend mutes.", ephemeral: true);
                return;
            }

            // For now, just show a message - in a full implementation, you'd show a modal to collect extension duration
            await FollowupAsync($"⏰ Mute extension for User {mute.UserId}. " +
                              "Full implementation would show a modal to collect extension duration.", ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"❌ An error occurred: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("mute-action:details:*")]
    public async Task HandleMuteDetailsAsync(string muteIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("❌ You cannot interact with this paginator.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(muteIdString, out var muteId))
            {
                await FollowupAsync("❌ Invalid mute ID.", ephemeral: true);
                return;
            }

            var mute = await Db.Set<Mute>()
                .Include(m => m.Action)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == muteId);

            if (mute == null)
            {
                await FollowupAsync("❌ Mute not found.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Mute Details - {mute.UserId}")
                .WithColor(Color.Orange)
                .AddField("User ID", mute.UserId, true)
                .AddField("Status", mute.Status.ToString(), true)
                .AddField("Duration", mute.Length?.Humanize() ?? "Permanent", true)
                .AddField("Reason", mute.Action?.Reason ?? "No reason provided", false)
                .AddField("Moderator", mute.Action?.Moderator is { } mod ? mod.Id.ToString() : "System", true)
                .AddField("Date", mute.Action?.Date.ToString("MMM dd, yyyy HH:mm") ?? "Unknown", true)
                .WithFooter($"Mute ID: {mute.Id}")
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (mute.Category != null)
                embed.AddField("Category", mute.Category.Name, true);

            await FollowupAsync(
                components: embed.Build().ToComponentsV2Message(),
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"❌ An error occurred: {ex.Message}", ephemeral: true);
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

        await paginator.RenderPageAsync(interaction);
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

        await DeferAsync(ephemeral: true);

        var state = paginator.GetUserState<MuteListPaginatorState>();
        var refreshedMutes = await RefreshMuteData(state.Category);
        state.UpdateData(refreshedMutes, state.Category);
        paginator.PageCount = state.TotalPages;

        await paginator.RenderPageAsync(interaction);
        await FollowupAsync("🔄 Mute list refreshed successfully.", ephemeral: true);
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
            .Where(r => category == null || r.Category?.Id == category.Id)
            .OrderByDescending(r => r.Action?.Date)
            .ToList();
    }

    // User History V2 Interaction Handlers
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

        await paginator.RenderPageAsync(interaction);
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

        await paginator.RenderPageAsync(interaction);
    }

    [ComponentInteraction("history-refresh")]
    public async Task HandleHistoryRefreshAsync()
    {
        var interaction = (IComponentInteraction)Context.Interaction;
        
        Console.WriteLine($"[DEBUG] history-refresh interaction received from {interaction.User.Username}");

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator))
        {
            Console.WriteLine("[DEBUG] TryGetComponentPaginator returned false");
            await DeferAsync();
            return;
        }
        
        if (!paginator.CanInteract(interaction.User))
        {
            Console.WriteLine("[DEBUG] CanInteract returned false");
            await DeferAsync();
            return;
        }

        await DeferAsync(ephemeral: true);

        var state = paginator.GetUserState<UserHistoryPaginatorState>();

        // Refresh data from database
        var guild = await Db.Guilds.Include(g => g.ReprimandHistory).FirstAsync(g => g.Id == Context.Guild.Id);
        var refreshedHistory = guild.ReprimandHistory
            .Where(r => r.UserId == state.User.Id)
            .OrderByDescending(r => r.Action?.Date)
            .ToList();

        state.UpdateData(refreshedHistory);
        paginator.PageCount = state.TotalPages;

        await paginator.RenderPageAsync(interaction);
        await FollowupAsync("🔄 User history refreshed successfully.", ephemeral: true);
    }

    [ComponentInteraction("history-expand:*")]
    public async Task HandleHistoryExpandCollapseAsync(string userIdString)
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
        
        // Toggle expanded state
        state.ToggleExpanded();
        
        // Update page count and adjust current page if needed
        paginator.PageCount = state.TotalPages;
        
        // If we were on a page that no longer exists, go to the last page
        if (paginator.CurrentPageIndex >= state.TotalPages)
        {
            paginator.SetPage(state.TotalPages - 1);
        }

        await paginator.RenderPageAsync(interaction);
    }

    [ComponentInteraction("history-group:*")]
    public async Task HandleHistoryGroupToggleAsync(string userIdString)
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
        
        // Toggle grouped state
        state.ToggleGrouped();

        await paginator.RenderPageAsync(interaction);
    }
}
