using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;

namespace HuTao.Services.Utilities;

/// <summary>
/// Helper class for parsing media URLs from text and detecting NSFW content
/// </summary>
public static class MediaParsingHelper
{
    /// <summary>
    /// Regex patterns for extracting image URLs from markdown and plain text
    /// </summary>
    private static readonly Regex[] ImagePatterns =
    [
        new Regex(@"!\[.*?\]\((https?://[^\)]+\.(?:jpg|jpeg|png|gif|webp)(?:\?[^\)]*)?)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"https?://[^\s<>]+\.(?:jpg|jpeg|png|gif|webp)(?:\?[^\s<>]*)?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"https?://(?:cdn\.)?discord(?:app)?\.com/attachments/\d+/\d+/[^\s<>]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    /// <summary>
    /// Keywords that indicate NSFW content - checks in both reason text and image URLs
    /// </summary>
    private static readonly HashSet<string> NsfwKeywords =
    [
        "nsfw", "nsfl", "spoiler", "gore", "blood", "explicit", "18+", "adult",
        "lewd", "sexual", "nude", "porn", "hentai", "xxx", "suggestive"
    ];

    /// <summary>
    /// Extracts all image URLs from text (markdown and plain text)
    /// </summary>
    /// <param name="text">Text to parse for image URLs</param>
    /// <returns>List of distinct image URLs found in the text</returns>
    public static List<string> ExtractImageUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var urls = new HashSet<string>();

        foreach (var pattern in ImagePatterns)
        {
            var matches = pattern.Matches(text);
            foreach (Match match in matches)
            {
                var url = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                urls.Add(url);
            }
        }

        return urls.ToList();
    }

    /// <summary>
    /// Determines if content should be marked as NSFW/spoiler
    /// Checks both the text content and image URLs for NSFW indicators
    /// </summary>
    /// <param name="text">Reason or content text to check</param>
    /// <param name="imageUrls">Optional list of image URLs to check</param>
    /// <returns>True if NSFW content is detected</returns>
    public static bool IsNsfwContent(string text, IEnumerable<string>? imageUrls = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Check text content (case-insensitive word boundaries)
        var textLower = text.ToLowerInvariant();
        if (NsfwKeywords.Any(keyword => textLower.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check image URLs if provided
        if (imageUrls != null)
        {
            foreach (var url in imageUrls)
            {
                var urlLower = url.ToLowerInvariant();
                if (NsfwKeywords.Any(keyword => urlLower.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates MediaGalleryItemProperties with appropriate spoiler setting
    /// </summary>
    /// <param name="imageUrl">Image URL</param>
    /// <param name="reason">Reason text (used for NSFW detection)</param>
    /// <returns>MediaGalleryItemProperties with spoiler flag set if NSFW detected</returns>
    public static MediaGalleryItemProperties CreateMediaItem(string imageUrl, string? reason = null)
    {
        var isNsfw = IsNsfwContent(reason ?? "", [imageUrl]);
        var unfurledMedia = new UnfurledMediaItemProperties(imageUrl);
        
        return new MediaGalleryItemProperties(unfurledMedia, isSpoiler: isNsfw);
    }

    /// <summary>
    /// Extracts image URLs and creates MediaGalleryItemProperties with NSFW detection
    /// </summary>
    /// <param name="text">Text to parse for images</param>
    /// <param name="reason">Reason text (used for NSFW detection)</param>
    /// <returns>List of MediaGalleryItemProperties</returns>
    public static List<MediaGalleryItemProperties> ExtractAndCreateMediaItems(string text, string? reason = null)
    {
        var imageUrls = ExtractImageUrls(text);
        return imageUrls.Select(url => CreateMediaItem(url, reason ?? text)).ToList();
    }

    /// <summary>
    /// Gets notes attached to a reprimand (notes with matching parent ID or similar context)
    /// </summary>
    /// <param name="reprimand">The main reprimand</param>
    /// <param name="allReprimands">All reprimands for the user</param>
    /// <returns>List of notes that appear to be attached to this reprimand</returns>
    public static List<Note> GetAttachedNotes(Reprimand reprimand, IEnumerable<Reprimand> allReprimands)
    {
        // Notes are considered "attached" if they:
        // 1. Are Note type
        // 2. Have the same category as the reprimand
        // 3. Occur within a short time window after the main reprimand (e.g., same day)
        // 4. Are from the same moderator or shortly after
        
        var reprimandDate = reprimand.Action?.Date ?? DateTimeOffset.UtcNow;
        var reprimandModId = reprimand.Action?.Moderator?.Id;
        
        return allReprimands
            .OfType<Note>()
            .Where(n => n.Id != reprimand.Id) // Don't include self if reprimand is a note
            .Where(n => n.CategoryId == reprimand.CategoryId)
            .Where(n =>
            {
                var noteDate = n.Action?.Date ?? DateTimeOffset.UtcNow;
                var timeDiff = noteDate - reprimandDate;
                
                // Note occurred after the reprimand within 24 hours
                return timeDiff.TotalSeconds >= 0 && timeDiff.TotalHours <= 24;
            })
            .Where(n =>
            {
                var noteMod = n.Action?.Moderator?.Id;
                // Same moderator or within short timeframe
                return noteMod == reprimandModId || 
                       (n.Action?.Date ?? DateTimeOffset.UtcNow) - reprimandDate < TimeSpan.FromMinutes(30);
            })
            .OrderBy(n => n.Action?.Date)
            .ToList();
    }
}




