using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    private static readonly Regex MarkdownImagePattern = new(
        @"!\[[^\]]*]\(\s*<?(?<url>https?://[^\s)]+)>?\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MarkdownLinkPattern = new(
        @"\[[^\]]*]\(\s*<?(?<url>https?://[^\s)]+)>?\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RawUrlPattern = new(
        @"https?://[^\s<>\)\]]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyCollection<string> ImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    ];

    private static readonly IReadOnlyCollection<string> DiscordCdnHosts =
    [
        "cdn.discordapp.com",
        "media.discordapp.net"
    ];

    private static readonly char[] TrailingUrlPunctuation =
    [
        '.', ',', '!', ';', ')', ']', '}', '>', '"', '\''
    ];

    private const int MaxReasonLength = 1024;

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

        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ExtractFromPattern(MarkdownImagePattern, text, urls);
        ExtractFromPattern(MarkdownLinkPattern, text, urls);
        ExtractFromPattern(RawUrlPattern, text, urls);

        return urls.Where(IsLikelyImageUrl).ToList();
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

    public static string? AppendAttachmentsToReason(string? reason, IEnumerable<IAttachment>? attachments)
    {
        if (attachments is null) return reason;

        var attachmentLines = BuildAttachmentLines(reason, attachments);
        if (attachmentLines.Count == 0) return reason;

        var baseReason = string.IsNullOrWhiteSpace(reason) ? "No reason provided" : reason.TrimEnd();
        return CombineReasonAndAttachments(baseReason, attachmentLines);
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

    private static void ExtractFromPattern(Regex pattern, string text, ISet<string> urls)
    {
        var matches = pattern.Matches(text);
        foreach (Match match in matches)
        {
            var url = match.Groups["url"].Success ? match.Groups["url"].Value : match.Value;
            var normalized = NormalizeUrl(url);
            if (!string.IsNullOrWhiteSpace(normalized))
                urls.Add(normalized);
        }
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim().Trim('<', '>');
        return trimmed.TrimEnd(TrailingUrlPunctuation);
    }

    private static bool IsLikelyImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath;
        if (ImageExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (!IsDiscordAttachment(uri))
            return false;

        return uri.Query.Contains("width=", StringComparison.OrdinalIgnoreCase)
            || uri.Query.Contains("height=", StringComparison.OrdinalIgnoreCase)
            || uri.Query.Contains("format=", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDiscordAttachment(Uri uri)
    {
        if (!DiscordCdnHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            return false;

        return uri.AbsolutePath.Contains("/attachments/", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> BuildAttachmentLines(string? reason, IEnumerable<IAttachment> attachments)
    {
        var lines = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reasonText = reason ?? string.Empty;

        foreach (var attachment in attachments.OrderByDescending(IsImageAttachment))
        {
            var url = NormalizeUrl(attachment.Url);
            if (string.IsNullOrWhiteSpace(url)) continue;
            if (!seen.Add(url)) continue;
            if (!string.IsNullOrEmpty(reasonText) &&
                reasonText.Contains(url, StringComparison.OrdinalIgnoreCase))
                continue;

            var label = string.IsNullOrWhiteSpace(attachment.Filename) ? "Attachment" : attachment.Filename;
            lines.Add($"-# - [{label}]({url})");
        }

        return lines;
    }

    private static string CombineReasonAndAttachments(string baseReason, IReadOnlyList<string> attachmentLines)
    {
        var attachmentsText = BuildAttachmentText(attachmentLines, MaxReasonLength);
        if (string.IsNullOrEmpty(attachmentsText))
            return TruncateToLength(baseReason, MaxReasonLength);

        if (attachmentsText.Length >= MaxReasonLength)
            return attachmentsText;

        var remainingForReason = MaxReasonLength - attachmentsText.Length - 1;
        if (remainingForReason <= 0)
            return attachmentsText;

        var truncatedReason = TruncateToLength(baseReason, remainingForReason);
        return $"{truncatedReason}\n{attachmentsText}";
    }

    private static string BuildAttachmentText(IReadOnlyList<string> lines, int maxLength)
    {
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            var extra = builder.Length == 0 ? line.Length : line.Length + 1;
            if (builder.Length + extra > maxLength)
                break;

            if (builder.Length > 0)
                builder.AppendLine();

            builder.Append(line);
        }

        return builder.ToString();
    }

    private static string TruncateToLength(string text, int maxLength)
    {
        if (maxLength <= 0) return string.Empty;
        if (text.Length <= maxLength) return text;
        if (maxLength <= 3) return text[..maxLength];
        return text[..(maxLength - 3)] + "...";
    }

    private static bool IsImageAttachment(IAttachment attachment)
    {
        if (attachment.Height is not null || attachment.Width is not null)
            return true;

        if (!string.IsNullOrWhiteSpace(attachment.ContentType) &&
            attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return true;

        var url = attachment.Url;
        return ImageExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}




