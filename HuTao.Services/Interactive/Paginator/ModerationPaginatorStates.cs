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
    private const int MaxTextPerPage = 3500;
    private const int MaxComponentsPerPage = 25;
    private const int OverheadPerItem = 100;

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
        FilteredReprimands = FilterReprimands();
        TotalReprimands = FilteredReprimands.Count;
        CalculatePageBoundaries();
    }

    public IUser User { get; }
    public GuildUserEntity UserEntity { get; }
    public IUser RequestedBy { get; }
    public byte[] HistoryImageBytes { get; }
    
    public IReadOnlyList<Reprimand> AllReprimands { get; private set; }
    public IReadOnlyList<Reprimand> FilteredReprimands { get; private set; }
    public ModerationCategory? CategoryFilter { get; set; }
    public LogReprimandType TypeFilter { get; set; }
    public GuildEntity Guild { get; }
    public int TotalReprimands { get; private set; }
    public int TotalPages { get; private set; }
    public bool PageCountChanged { get; private set; }
    
    private List<int> PageStartIndices { get; set; } = new() { 0 };

    public IEnumerable<Reprimand> GetReprimandsForPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= PageStartIndices.Count)
            return Enumerable.Empty<Reprimand>();
        
        var startIndex = PageStartIndices[pageIndex];
        var endIndex = pageIndex + 1 < PageStartIndices.Count 
            ? PageStartIndices[pageIndex + 1] 
            : FilteredReprimands.Count;
        
        return FilteredReprimands.Skip(startIndex).Take(endIndex - startIndex);
    }

    public void UpdateFilters(ModerationCategory? category, LogReprimandType type)
    {
        CategoryFilter = category;
        TypeFilter = type;
        FilteredReprimands = FilterReprimands();
        TotalReprimands = FilteredReprimands.Count;
        
        var oldPages = TotalPages;
        CalculatePageBoundaries();
        PageCountChanged = TotalPages != oldPages;
    }

    public void UpdateData(IReadOnlyList<Reprimand> reprimands)
    {
        AllReprimands = reprimands;
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
    
    private void CalculatePageBoundaries()
    {
        PageStartIndices = new List<int> { 0 };
        
        if (!FilteredReprimands.Any())
        {
            TotalPages = 1;
            return;
        }
        
        var currentPageTextLength = 0;
        var currentComponentCount = 0;
        
        for (int i = 0; i < FilteredReprimands.Count; i++)
        {
            var reprimand = FilteredReprimands[i];
            var reasonLength = reprimand.Action?.Reason?.Length ?? 20;
            var estimatedLength = reasonLength + OverheadPerItem;
            
            var wouldExceedText = currentPageTextLength + estimatedLength > MaxTextPerPage;
            var wouldExceedComponents = currentComponentCount + 1 > MaxComponentsPerPage;
            
            if ((wouldExceedText || wouldExceedComponents) && currentPageTextLength > 0)
            {
                PageStartIndices.Add(i);
                currentPageTextLength = estimatedLength;
                currentComponentCount = 1;
            }
            else
            {
                currentPageTextLength += estimatedLength;
                currentComponentCount++;
            }
        }
        
        TotalPages = PageStartIndices.Count;
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
