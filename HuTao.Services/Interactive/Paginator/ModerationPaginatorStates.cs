using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Moderation;

namespace HuTao.Services.Interactive.Paginator;

/// <summary>
/// State management class for the mute list paginator in Components V2
/// </summary>
public class MuteListPaginatorState
{
    private const int MutesPerPage = 3; // Reduced for Components V2 to fit interaction buttons

    public MuteListPaginatorState(IReadOnlyList<Mute> mutes, ModerationCategory? category, GuildEntity guild)
    {
        AllMutes = mutes;
        Category = category;
        Guild = guild;
        TotalMutes = mutes.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalMutes / MutesPerPage));
    }

    public IReadOnlyList<Mute> AllMutes { get; private set; }
    public ModerationCategory? Category { get; set; }
    public GuildEntity Guild { get; }
    public int TotalMutes { get; private set; }
    public int TotalPages { get; private set; }

    public IEnumerable<Mute> GetMutesForPage(int pageIndex)
        => AllMutes.Skip(pageIndex * MutesPerPage).Take(MutesPerPage);

    public void UpdateCategory(ModerationCategory? newCategory)
    {
        Category = newCategory;
        // Note: Would need to refresh data from DB for filtering
    }

    public void UpdateData(IReadOnlyList<Mute> mutes, ModerationCategory? category)
    {
        AllMutes = mutes;
        Category = category;
        TotalMutes = mutes.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalMutes / MutesPerPage));
    }

    public MuteDisplayInfo GetMuteDisplayInfo(Mute mute)
    {
        var duration = mute.Length?.Humanize() ?? "Permanent";
        var expiry = mute.Length != null
            ? $"<t:{((DateTimeOffset)(mute.StartedAt + mute.Length)).ToUnixTimeSeconds()}:R>"
            : "Never";

        // Get user info from cache or use fallback
        var username = $"User {mute.UserId}"; // Should integrate with user cache
        var avatarUrl = "https://cdn.discordapp.com/embed/avatars/0.png"; // Default avatar

        return new MuteDisplayInfo
        {
            Username = username,
            AvatarUrl = avatarUrl,
            Reason = mute.Action?.Reason?.Truncate(100) ?? "No reason provided",
            Duration = duration,
            ExpiryDisplay = expiry
        };
    }

    public record MuteDisplayInfo
    {
        public string Username { get; init; } = "";
        public string AvatarUrl { get; init; } = "";
        public string Reason { get; init; } = "";
        public string Duration { get; init; } = "";
        public string ExpiryDisplay { get; init; } = "";
    }
}

/// <summary>
/// State management class for user history paginator in Components V2
/// </summary>
public class UserHistoryPaginatorState
{
    private const int CollapsedReprimandsPerPage = 5;
    private const int ExpandedReprimandsPerPage = 10;

    public UserHistoryPaginatorState(IUser user, GuildUserEntity userEntity, IReadOnlyList<Reprimand> reprimands,
                                   ModerationCategory? category, LogReprimandType typeFilter, GuildEntity guild, IUser requestedBy, byte[] historyImageBytes)
    {
        User = user;
        UserEntity = userEntity;
        AllReprimands = reprimands;
        CategoryFilter = category;
        TypeFilter = typeFilter;
        Guild = guild;
        RequestedBy = requestedBy;
        HistoryImageBytes = historyImageBytes;
        IsChronologicalExpanded = false; // Default to collapsed (5 items per page)
        IsGroupedExpanded = false; // Default to collapsed (truncated reasons)
        FilteredReprimands = FilterReprimands();
        TotalReprimands = FilteredReprimands.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalReprimands / ReprimandsPerPage));
    }

    public IUser User { get; }
    public GuildUserEntity UserEntity { get; }
    public IUser RequestedBy { get; }
    public byte[] HistoryImageBytes { get; }
    public bool IsChronologicalExpanded { get; set; }
    public bool IsGroupedExpanded { get; set; }
    public bool IsGrouped { get; set; }
    
    private int ReprimandsPerPage
    {
        get
        {
            // Grouped mode: Calculate based on 4000 char limit per page
            if (IsGrouped)
                return CalculateGroupedReprimandsPerPage();
            
            // Chronological mode uses 5 or 10 per page
            return IsChronologicalExpanded ? ExpandedReprimandsPerPage : CollapsedReprimandsPerPage;
        }
    }
    
    /// <summary>
    /// Calculates how many reprimands fit on a page in grouped mode based on 4000 char limit
    /// </summary>
    private int CalculateGroupedReprimandsPerPage()
    {
        // IMPORTANT: This is the TOTAL MESSAGE TEXT limit across ALL components
        // Discord has a 4000 char limit for total displayable text in a message
        // Use 3500 to leave room for header, footer, buttons, and other UI elements
        const int maxTotalMessageText = 3500;
        const int overheadPerEntry = 50; // Estimate for "• " + " • <t:timestamp:d>\n"
        
        var totalTextLength = 0;
        var count = 0;
        
        // Group reprimands by type using the proper title
        var grouped = FilteredReprimands
            .GroupBy(r => r.GetTitle(showId: false))
            .OrderBy(g => g.Key);
        
        foreach (var group in grouped)
        {
            // Header: "### TypeName\n"
            var headerLength = group.Key.Length + 5;
            
            // Check if adding this group header would exceed TOTAL MESSAGE limit
            if (totalTextLength + headerLength > maxTotalMessageText && count > 0)
                break;
            
            totalTextLength += headerLength;
            
            foreach (var reprimand in group.OrderByDescending(r => r.Action?.Date))
            {
                var reason = reprimand.Action?.Reason ?? "No reason provided";
                var entryLength = (IsGroupedExpanded ? reason.Length : Math.Min(reason.Length, 80)) + overheadPerEntry;
                
                // Check if adding this entry would exceed TOTAL MESSAGE limit
                if (totalTextLength + entryLength > maxTotalMessageText && count > 0)
                    return count;
                
                totalTextLength += entryLength;
                count++;
            }
        }
        
        // If we processed all reprimands without hitting the limit, return total count
        return Math.Max(count, 1); // At least 1
    }
    public IReadOnlyList<Reprimand> AllReprimands { get; private set; }
    public IReadOnlyList<Reprimand> FilteredReprimands { get; private set; }
    public ModerationCategory? CategoryFilter { get; set; }
    public LogReprimandType TypeFilter { get; set; }
    public GuildEntity Guild { get; }
    public int TotalReprimands { get; private set; }
    public int TotalPages { get; private set; }
    public bool PageCountChanged { get; private set; }
    
    /// <summary>
    /// Gets the number of reprimands that fit on a page based on the current display mode
    /// </summary>
    public int GetReprimandsPerPage() => ReprimandsPerPage;

    public IEnumerable<Reprimand> GetReprimandsForPage(int pageIndex)
        => FilteredReprimands.Skip(pageIndex * ReprimandsPerPage).Take(ReprimandsPerPage);

    public void UpdateFilters(ModerationCategory? category, LogReprimandType type)
    {
        CategoryFilter = category;
        TypeFilter = type;
        FilteredReprimands = FilterReprimands();
        var newTotal = FilteredReprimands.Count;
        var newPages = Math.Max(1, (int)Math.Ceiling((double)newTotal / ReprimandsPerPage));

        PageCountChanged = newPages != TotalPages;
        TotalReprimands = newTotal;
        TotalPages = newPages;
    }
    
    public void ToggleExpanded()
    {
        IsChronologicalExpanded = !IsChronologicalExpanded;
        var newPages = Math.Max(1, (int)Math.Ceiling((double)TotalReprimands / ReprimandsPerPage));
        PageCountChanged = newPages != TotalPages;
        TotalPages = newPages;
    }
    
    public void ToggleGrouped()
    {
        IsGrouped = !IsGrouped;
        
        // Recalculate page count: grouped mode shows all on one page, chronological uses pagination
        var newPages = Math.Max(1, (int)Math.Ceiling((double)TotalReprimands / ReprimandsPerPage));
        PageCountChanged = newPages != TotalPages;
        TotalPages = newPages;
    }

    public void UpdateData(IReadOnlyList<Reprimand> reprimands)
    {
        AllReprimands = reprimands;
        // Re-apply current filters to compute counts and pages
        UpdateFilters(CategoryFilter, TypeFilter);
    }

    private IReadOnlyList<Reprimand> FilterReprimands()
    {
        var filtered = AllReprimands
            .OfCategory(CategoryFilter ?? ModerationCategory.All)
            .OfType(TypeFilter)
            .ToList();

        return filtered;
    }

    public ReprimandDisplayInfo GetReprimandDisplayInfo(Reprimand reprimand)
    {
        var actionDate = reprimand.Action?.Date ?? DateTimeOffset.UtcNow;
        var shortId = reprimand.Id.ToString().Split('-')[0]; // First segment of GUID
        
        return new ReprimandDisplayInfo
        {
            Type = reprimand.GetTitle(showId: false),
            TypeBadges = Utilities.ReprimandBadgeHelper.GetTypeBadges(reprimand),
            StatusBadge = Utilities.ReprimandBadgeHelper.GetStatusBadge(reprimand.Status),
            ShortId = shortId,
            Moderator = reprimand.Action?.Moderator is { } mod ? $"<@{mod.Id}>" : "System",
            DateShort = $"<t:{actionDate.ToUnixTimeSeconds()}:d>",
            TimeShort = $"<t:{actionDate.ToUnixTimeSeconds()}:t>",
            RelativeTime = $"<t:{actionDate.ToUnixTimeSeconds()}:R>",
            Reason = reprimand.Action?.Reason ?? "No reason provided"
        };
    }

    public record ReprimandDisplayInfo
    {
        public string Type { get; init; } = "";
        public string TypeBadges { get; init; } = ""; // Shield.io badges for visual display
        public string StatusBadge { get; init; } = ""; // Status indicator badge
        public string ShortId { get; init; } = "";
        public string Moderator { get; init; } = "";
        public string DateShort { get; init; } = "";
        public string TimeShort { get; init; } = "";
        public string RelativeTime { get; init; } = "";
        public string Reason { get; init; } = "";
    }
}
