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
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Timeout = HuTao.Data.Models.Moderation.Infractions.Reprimands.Timeout;

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
    public ILogger<ModerationInteractionHandlers> Log { get; init; } = null!;

    [ComponentInteraction("mute-action:unmute:*")]
    public async Task HandleMuteUnmuteAsync(string muteIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("❌ Only the person who opened this can interact with it.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(muteIdString, out var muteId))
            {
                await FollowupAsync("❌ Could not find that mute record.", ephemeral: true);
                return;
            }

            var mute = await Db.Set<Mute>().FirstOrDefaultAsync(m => m.Id == muteId);
            if (mute == null)
            {
                await FollowupAsync("❌ This mute was already removed or doesn't exist.", ephemeral: true);
                return;
            }

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
                await FollowupAsync($"✅ **<@{user.Id}>** has been unmuted.", ephemeral: true);
            }
            else
            {
                await FollowupAsync("❌ Could not unmute this user. They may no longer be muted.", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error unmuting user {MuteId}", muteIdString);
            await FollowupAsync("❌ Something went wrong. Please try again or contact an admin.", ephemeral: true);
        }
    }

    [ComponentInteraction("mute-action:extend:*")]
    public async Task HandleMuteExtendAsync(string muteIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("❌ Only the person who opened this can interact with it.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(muteIdString, out var muteId))
            {
                await FollowupAsync("❌ Could not find that mute record.", ephemeral: true);
                return;
            }

            var mute = await Db.Set<Mute>().FirstOrDefaultAsync(m => m.Id == muteId);
            if (mute == null)
            {
                await FollowupAsync("❌ This mute was already removed or doesn't exist.", ephemeral: true);
                return;
            }

            var hasPermission = await Auth.IsAuthorizedAsync(Context, AuthorizationScope.Mute);
            if (!hasPermission)
            {
                await FollowupAsync("❌ You don't have permission to extend mutes.", ephemeral: true);
                return;
            }

            await FollowupAsync($"⏰ Mute extension for <@{mute.UserId}>. " +
                              "Full implementation would show a modal to collect extension duration.", ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error extending mute {MuteId}", muteIdString);
            await FollowupAsync("❌ Something went wrong. Please try again or contact an admin.", ephemeral: true);
        }
    }

    [ComponentInteraction("mute-action:details:*")]
    public async Task HandleMuteDetailsAsync(string muteIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("❌ Only the person who opened this can interact with it.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(muteIdString, out var muteId))
            {
                await FollowupAsync("❌ Could not find that mute record.", ephemeral: true);
                return;
            }

            var mute = await Db.Set<Mute>()
                .Include(m => m.Action)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == muteId);

            if (mute == null)
            {
                await FollowupAsync("❌ This mute was already removed or doesn't exist.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Mute Details - <@{mute.UserId}>")
                .WithColor(Color.Orange)
                .AddField("User ID", mute.UserId, true)
                .AddField("Status", mute.Status.ToString(), true)
                .AddField("Duration", mute.Length?.Humanize() ?? "Permanent", true)
                .AddField("Reason", mute.Action?.Reason ?? "No reason provided")
                .AddField("Moderator", mute.Action?.Moderator is { } mod ? $"<@{mod.Id}>" : "System", true)
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
            Log.LogError(ex, "Error fetching mute details {MuteId}", muteIdString);
            await FollowupAsync("❌ Something went wrong. Please try again or contact an admin.", ephemeral: true);
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
        await FollowupAsync("🔄 Mute list updated.", ephemeral: true);
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

    [ComponentInteraction("timeout-action:remove:*")]
    public async Task HandleTimeoutRemoveAsync(string timeoutIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("❌ Only the person who opened this can interact with it.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(timeoutIdString, out var timeoutId))
            {
                await FollowupAsync("❌ Could not find that timeout record.", ephemeral: true);
                return;
            }

            var timeout = await Db.Set<Timeout>().FirstOrDefaultAsync(t => t.Id == timeoutId);
            if (timeout == null)
            {
                await FollowupAsync("❌ This timeout was already removed or doesn't exist.", ephemeral: true);
                return;
            }

            var hasPermission = await Auth.IsAuthorizedAsync(Context, AuthorizationScope.Timeout);
            if (!hasPermission)
            {
                await FollowupAsync("❌ You don't have permission to remove timeouts.", ephemeral: true);
                return;
            }

            var user = await Context.Client.Rest.GetUserAsync(timeout.UserId);
            var details = new ReprimandDetails(user, (IGuildUser)Context.User, "Manual timeout removal via paginator");

            var result = await ModerationService.TryUntimeoutAsync(details);
            if (result)
            {
                var state = paginator.GetUserState<TimeoutListPaginatorState>();
                var refreshed = await RefreshTimeoutData(state.Category);
                state.UpdateData(refreshed, state.Category);
                paginator.PageCount = state.TotalPages;

                await paginator.RenderPageAsync(interaction);
                await FollowupAsync($"✅ **<@{user.Id}>** timeout has been removed.", ephemeral: true);
            }
            else
            {
                await FollowupAsync("❌ Could not remove timeout. The user may not be timed out.", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error removing timeout {TimeoutId}", timeoutIdString);
            await FollowupAsync("❌ Something went wrong. Please try again or contact an admin.", ephemeral: true);
        }
    }

    [ComponentInteraction("timeout-action:details:*")]
    public async Task HandleTimeoutDetailsAsync(string timeoutIdString)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await RespondAsync("❌ Only the person who opened this can interact with it.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            if (!Guid.TryParse(timeoutIdString, out var timeoutId))
            {
                await FollowupAsync("❌ Could not find that timeout record.", ephemeral: true);
                return;
            }

            var timeout = await Db.Set<Timeout>()
                .Include(t => t.Action)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == timeoutId);

            if (timeout == null)
            {
                await FollowupAsync("❌ This timeout was already removed or doesn't exist.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Timeout Details - <@{timeout.UserId}>")
                .WithColor(Color.Orange)
                .AddField("User ID", timeout.UserId, true)
                .AddField("Status", timeout.Status.ToString(), true)
                .AddField("Duration", timeout.Length?.Humanize() ?? "Permanent", true)
                .AddField("Reason", timeout.Action?.Reason ?? "No reason provided")
                .AddField("Moderator", timeout.Action?.Moderator is { } mod ? $"<@{mod.Id}>" : "System", true)
                .AddField("Date", timeout.Action?.Date.ToString("MMM dd, yyyy HH:mm") ?? "Unknown", true)
                .WithFooter($"Timeout ID: {timeout.Id}")
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (timeout.Category != null)
                embed.AddField("Category", timeout.Category.Name, true);

            await FollowupAsync(
                components: embed.Build().ToComponentsV2Message(),
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error fetching timeout details {TimeoutId}", timeoutIdString);
            await FollowupAsync("❌ Something went wrong. Please try again or contact an admin.", ephemeral: true);
        }
    }

    [ComponentInteraction("timeout-category-filter")]
    public async Task HandleTimeoutCategoryFilterAsync(string categoryValue)
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await DeferAsync();
            return;
        }

        await DeferAsync();

        var state = paginator.GetUserState<TimeoutListPaginatorState>();

        ModerationCategory? newCategory = null;
        if (categoryValue != "all" && Guid.TryParse(categoryValue, out var categoryGuid))
        {
            newCategory = state.Guild.ModerationCategories.FirstOrDefault(c => c.Id == categoryGuid);
        }

        var filtered = await RefreshTimeoutData(newCategory);
        state.UpdateData(filtered, newCategory);
        paginator.PageCount = state.TotalPages;
        paginator.SetPage(0);

        await paginator.RenderPageAsync(interaction);
    }

    [ComponentInteraction("timeout-refresh")]
    public async Task HandleTimeoutRefreshAsync()
    {
        var interaction = (IComponentInteraction)Context.Interaction;

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator) ||
            !paginator.CanInteract(interaction.User))
        {
            await DeferAsync();
            return;
        }

        await DeferAsync(ephemeral: true);

        var state = paginator.GetUserState<TimeoutListPaginatorState>();
        var refreshed = await RefreshTimeoutData(state.Category);
        state.UpdateData(refreshed, state.Category);
        paginator.PageCount = state.TotalPages;

        await paginator.RenderPageAsync(interaction);
        await FollowupAsync("🔄 Timeout list updated.", ephemeral: true);
    }

    private async Task<IReadOnlyList<Timeout>> RefreshTimeoutData(ModerationCategory? category)
    {
        var guild = await Db.Guilds.Include(g => g.ReprimandHistory).FirstAsync(g => g.Id == Context.Guild.Id);

        return guild.ReprimandHistory.OfType<Timeout>()
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

        if (!Interactive.TryGetComponentPaginator(interaction.Message, out var paginator))
        {
            Log.LogDebug("history-refresh ignored: paginator not found (UserId={UserId}, MessageId={MessageId})",
                interaction.User.Id, interaction.Message.Id);
            await DeferAsync();
            return;
        }
        
        if (!paginator.CanInteract(interaction.User))
        {
            Log.LogDebug("history-refresh ignored: user cannot interact (UserId={UserId}, MessageId={MessageId})",
                interaction.User.Id, interaction.Message.Id);
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
        await FollowupAsync("🔄 History updated.", ephemeral: true);
    }
}
