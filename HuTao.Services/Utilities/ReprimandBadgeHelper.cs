using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Moderation;

namespace HuTao.Services.Utilities;

/// <summary>
/// Helper class for generating shield.io badges for reprimand types and statuses
/// with consistent sizing and color coding
/// </summary>
public static class ReprimandBadgeHelper
{
    private const string ShieldBaseUrl = "https://img.shields.io/badge";
    private const int MaxCharsPerBadge = 4; // Keep badges small for consistent size

    /// <summary>
    /// Color scheme for reprimand types
    /// </summary>
    private static readonly Dictionary<Type, string> TypeColors = new()
    {
        [typeof(Warning)] = "FFA500",      // Orange
        [typeof(Mute)] = "DC143C",         // Crimson Red
        [typeof(HardMute)] = "8B0000",     // Dark Red
        [typeof(Ban)] = "2C3E50",          // Dark Blue-Gray
        [typeof(Kick)] = "FF6347",         // Tomato
        [typeof(Timeout)] = "FF7F50",      // Coral
        [typeof(Notice)] = "3498DB",       // Blue
        [typeof(Note)] = "95A5A6",         // Gray
        [typeof(Censored)] = "9B59B6",     // Purple
        [typeof(Filtered)] = "E67E22",     // Carrot Orange
        [typeof(RoleReprimand)] = "1ABC9C" // Turquoise
    };

    /// <summary>
    /// Color scheme for reprimand statuses
    /// </summary>
    private static readonly Dictionary<ReprimandStatus, (string Color, string Icon)> StatusColors = new()
    {
        [ReprimandStatus.Added] = ("2ECC71", "✓"),        // Green - newly added
        [ReprimandStatus.Updated] = ("3498DB", "↻"),      // Blue - updated
        [ReprimandStatus.Expired] = ("95A5A6", "○"),      // Gray - expired
        [ReprimandStatus.Pardoned] = ("1ABC9C", "✓"),     // Turquoise - pardoned
        [ReprimandStatus.Deleted] = ("E74C3C", "✗"),      // Red - deleted
        [ReprimandStatus.Unknown] = ("7F8C8D", "?")       // Dark Gray - unknown
    };

    /// <summary>
    /// Generates a shield.io badge URL with proper encoding
    /// </summary>
    private static string CreateBadge(string text, string color, string? icon = null)
    {
        // Encode text for URL (spaces become underscores for shields.io)
        var encodedText = text.Replace(" ", "_");
        
        if (!string.IsNullOrEmpty(icon))
        {
            encodedText = $"{icon}_{encodedText}";
        }
        
        return $"{ShieldBaseUrl}/{encodedText}-{color}?style=flat";
    }

    /// <summary>
    /// Splits text into chunks for consistent badge sizing
    /// Discord displays inline images at consistent height, so splitting text maintains visual consistency
    /// </summary>
    private static IEnumerable<string> SplitForBadges(string text)
    {
        // For very short text, keep as-is
        if (text.Length <= MaxCharsPerBadge)
        {
            yield return text;
            yield break;
        }

        // Split intelligently at syllable boundaries for readability
        var chunks = new List<string>();
        var remaining = text;

        // Pre-defined splits for common reprimand types
        var knownSplits = new Dictionary<string, string[]>
        {
            ["Warning"] = new[] { "Warn", "ing" },
            ["HardMute"] = new[] { "Hard", "Mute" },
            ["Hard Mute"] = new[] { "Hard", "Mute" },
            ["Timeout"] = new[] { "Time", "out" },
            ["Notice"] = new[] { "Notice" }, // Short enough
            ["Censored"] = new[] { "Cens", "ored" },
            ["Filtered"] = new[] { "Filt", "ered" },
            ["Role Reprimand"] = new[] { "Role", "Rep" }
        };

        if (knownSplits.TryGetValue(text, out var predefinedChunks))
        {
            foreach (var chunk in predefinedChunks)
                yield return chunk;
            yield break;
        }

        // Fallback: split at MaxCharsPerBadge
        for (int i = 0; i < text.Length; i += MaxCharsPerBadge)
        {
            var chunk = text.Substring(i, Math.Min(MaxCharsPerBadge, text.Length - i));
            yield return chunk;
        }
    }

    /// <summary>
    /// Generates markdown image badges for a reprimand type
    /// Returns multiple badges if text needs to be split for consistent sizing
    /// </summary>
    public static string GetTypeBadges(Reprimand reprimand)
    {
        var type = reprimand.GetType();
        var typeName = reprimand.GetTitle(showId: false);
        var color = TypeColors.TryGetValue(type, out var c) ? c : "95A5A6";

        var chunks = SplitForBadges(typeName).ToList();
        
        // Generate badge for each chunk
        var badges = chunks.Select(chunk => 
        {
            var badgeUrl = CreateBadge(chunk, color);
            return $"![{chunk}]({badgeUrl})";
        });

        return string.Join("", badges);
    }

    /// <summary>
    /// Generates a status badge with icon
    /// </summary>
    public static string GetStatusBadge(ReprimandStatus status)
    {
        if (!StatusColors.TryGetValue(status, out var statusInfo))
            statusInfo = ("95A5A6", "?");

        var badgeUrl = CreateBadge(status.ToString(), statusInfo.Color, statusInfo.Icon);
        return $"![{status}]({badgeUrl})";
    }

    /// <summary>
    /// Generates combined type and status badges for a reprimand
    /// </summary>
    public static string GetCombinedBadges(Reprimand reprimand)
    {
        var typeBadges = GetTypeBadges(reprimand);
        var statusBadge = GetStatusBadge(reprimand.Status);
        
        return $"{typeBadges} {statusBadge}";
    }

    /// <summary>
    /// Gets just the type badge without status (for headers, summaries, etc.)
    /// </summary>
    public static string GetTypeOnly(Reprimand reprimand)
    {
        return GetTypeBadges(reprimand);
    }

    /// <summary>
    /// Gets a compact single-badge representation (no splitting)
    /// Useful for very space-constrained areas
    /// </summary>
    public static string GetCompactBadge(Reprimand reprimand, bool includeStatus = false)
    {
        var type = reprimand.GetType();
        var typeName = reprimand.GetTitle(showId: false);
        var color = TypeColors.TryGetValue(type, out var c) ? c : "95A5A6";

        string? icon = null;
        if (includeStatus && StatusColors.TryGetValue(reprimand.Status, out var statusInfo))
        {
            icon = statusInfo.Icon;
        }

        var badgeUrl = CreateBadge(typeName, color, icon);
        return $"![{typeName}]({badgeUrl})";
    }

    /// <summary>
    /// Gets color hex for a reprimand type (for other uses like embeds)
    /// </summary>
    public static string GetTypeColor(Reprimand reprimand)
    {
        var type = reprimand.GetType();
        return TypeColors.TryGetValue(type, out var color) ? color : "95A5A6";
    }

    /// <summary>
    /// Gets color hex for a status (for other uses like embeds)
    /// </summary>
    public static string GetStatusColor(ReprimandStatus status)
    {
        return StatusColors.TryGetValue(status, out var info) ? info.Color : "95A5A6";
    }
}

