using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Moderation;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharpColor = SixLabors.ImageSharp.Color;

namespace HuTao.Services.Image;

/// <summary>
///     Service for generating reprimand history images.
/// </summary>
public interface IReprimandHistoryImageService
{
    /// <summary>
    ///     Generates a reprimand history image for a user.
    /// </summary>
    /// <param name="user">The user entity containing reprimand data.</param>
    /// <param name="category">The moderation category to filter by.</param>
    /// <returns>A memory stream containing the generated PNG image.</returns>
    Task<MemoryStream> GenerateHistoryImageAsync(GuildUserEntity user, ModerationCategory? category);
}

public sealed class ReprimandHistoryImageService : IReprimandHistoryImageService
{
    public async Task<MemoryStream> GenerateHistoryImageAsync(GuildUserEntity user, ModerationCategory? category)
    {
        var sw = new Stopwatch();
        sw.Start();
        
        var scale = 2f;
        var baseWidth = 520;
        var headerHeight = (int) (32 * scale);
        var rowHeight = (int) (44 * scale);
        var iconSize = (int) (24 * scale);
        var iconGap = (int) (10 * scale);
        var padding = (int) (16 * scale);

        var reprimandData = GetReprimandData(user, category);

        var calculatedHeight = headerHeight + rowHeight * reprimandData.Length;
        var width = (int) (baseWidth * scale);
        var height = calculatedHeight;

        var backgroundColor = ImageSharpColor.Transparent;
        var headerColor = ImageSharpColor.ParseHex("5865F2");
        var rowEvenColor = ImageSharpColor.FromRgba(47, 49, 54, 220);
        var rowOddColor = ImageSharpColor.FromRgba(54, 57, 63, 220);
        var textColor = ImageSharpColor.White;
        var textSecondary = ImageSharpColor.ParseHex("DCDDDE");
        var textMuted = ImageSharpColor.ParseHex("8E9297");
        var iconColor = ImageSharpColor.ParseHex("B9BBBE");

        FontFamily textFont;
        FontFamily titleFontFamily;
        FontFamily iconFont;

        try
        {
            textFont        = SystemFonts.Get("Segoe UI");
            titleFontFamily = SystemFonts.Get("Segoe UI");
            iconFont        = SystemFonts.Get("Font Awesome 6 Free Solid");
        }
        catch
        {
            textFont        = SystemFonts.Get("Arial");
            titleFontFamily = SystemFonts.Get("Arial");
            iconFont        = SystemFonts.Get("Arial");
        }

        var headerFont = titleFontFamily.CreateFont(13 * scale, FontStyle.Bold);
        var labelFont = textFont.CreateFont(14 * scale, FontStyle.Regular);
        var countFont = textFont.CreateFont(14 * scale, FontStyle.Bold);
        var smallFont = textFont.CreateFont(12 * scale, FontStyle.Regular);
        var reasonFont = textFont.CreateFont(11 * scale, FontStyle.Regular);
        var iconFontSize = iconFont.CreateFont(18 * scale, FontStyle.Regular);

        var iconX = padding;
        var typeX = iconX + iconSize + iconGap;
        var countX = 160 * scale;
        var actionX = 240 * scale;
        var reasonX = 380 * scale;

        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx =>
        {
            ctx.Fill(backgroundColor);
            ctx.Fill(headerColor, new RectangleF(0, 0, width, headerHeight));

            for (var i = 0; i < reprimandData.Length; i++)
            {
                float rowY = headerHeight + i * rowHeight;
                var bg = i % 2 == 0 ? rowEvenColor : rowOddColor;
                ctx.Fill(bg, new RectangleF(0, rowY, width, rowHeight));
            }

            var headY = (headerHeight - headerFont.Size) / 2f;
            ctx.DrawText("Type", headerFont, textColor, new PointF(typeX, headY));
            ctx.DrawText("Count", headerFont, textColor, new PointF(countX, headY));
            ctx.DrawText("Last Action", headerFont, textColor, new PointF(actionX, headY));
            ctx.DrawText("Latest Reason", headerFont, textColor, new PointF(reasonX, headY));

            for (var i = 0; i < reprimandData.Length; i++)
            {
                var data = reprimandData[i];
                float rowY = headerHeight + i * rowHeight;
                var textY = rowY + (rowHeight - labelFont.Size) / 2f;
                var iconY = rowY + (rowHeight - iconFontSize.Size) / 2f;

                ctx.DrawText(data.Icon, iconFontSize, iconColor, new PointF(iconX, iconY));
                ctx.DrawText(data.Label, labelFont, textColor, new PointF(typeX, textY));

                var activeTextColor = data.Active > 0 ? textColor : textMuted;
                ctx.DrawText($"{data.Active}", countFont, activeTextColor, new PointF(countX, textY));
                
                var activeWidth = $"{data.Active}".Length * 9 * scale;
                ctx.DrawText($"/{data.Total}", smallFont, textMuted, new PointF(countX + activeWidth, textY + 2 * scale));

                ctx.DrawText(data.LastIssued, smallFont, textSecondary, new PointF(actionX, textY));

                var maxReasonLen = 28;
                var reasonText = data.LatestReason.Length > maxReasonLen 
                    ? data.LatestReason[..(maxReasonLen - 3)] + "..." 
                    : data.LatestReason;
                ctx.DrawText(reasonText, reasonFont, textSecondary, new PointF(reasonX, textY));
            }
        });

        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        sw.Stop();
        Debug.WriteLine($"Generated history image in {sw.Elapsed.Humanize()}");

        return stream;
    }

    private static ReprimandRowData[] GetReprimandData(GuildUserEntity user, ModerationCategory? category)
    {
        var rules = category?.Logging?.SummaryReprimands
            ?? user.Guild.ModerationRules?.Logging?.SummaryReprimands
            ?? LogReprimandType.All;

        var data = new List<ReprimandRowData>();

        if (rules.HasFlag(LogReprimandType.Warning))
            data.Add(GetReprimandRowData<Warning>("Warning", "\uf071", user, category));

        if (rules.HasFlag(LogReprimandType.Ban))
            data.Add(GetReprimandRowData<Ban>("Ban", "\uf05e", user, category));

        if (rules.HasFlag(LogReprimandType.Kick))
            data.Add(GetReprimandRowData<Kick>("Kick", "\uf54c", user, category));

        if (rules.HasFlag(LogReprimandType.Note))
            data.Add(GetReprimandRowData<Note>("Note", "\uf249", user, category));

        if (rules.HasFlag(LogReprimandType.Mute))
            data.Add(GetReprimandRowData<Mute>("Mute", "\uf131", user, category));

        if (rules.HasFlag(LogReprimandType.Timeout))
            data.Add(GetReprimandRowData<Timeout>("Timeout", "\uf251", user, category));

        if (rules.HasFlag(LogReprimandType.Censored))
            data.Add(GetReprimandRowData<Censored>("Censored", "\uf070", user, category));

        if (rules.HasFlag(LogReprimandType.Notice))
            data.Add(GetReprimandRowData<Notice>("Notice", "\uf0a1", user, category));

        return data
            .OrderByDescending(x => x.LastDate.HasValue ? 1 : 0)
            .ThenByDescending(x => x.LastDate ?? DateTimeOffset.MinValue)
            .ToArray();
    }
    
    private static ReprimandRowData GetReprimandRowData<T>(string label, string icon, GuildUserEntity user, ModerationCategory? category) 
        where T : Reprimand
    {
        var count = typeof(T) == typeof(Warning)
            ? user.WarningCount(category)
            : user.HistoryCount<T>(category);

        var latest = user.Reprimands<T>(category).OrderByDescending(r => r.Action?.Date).FirstOrDefault();

        return new ReprimandRowData(
            label, 
            icon, 
            (int)count.Active, 
            (int)count.Total, 
            latest?.Action?.Date.Humanize() ?? "-", 
            latest?.GetLatestReason() ?? "-",
            latest?.Action?.Date);
    }
    
    private record ReprimandRowData(
        string Label, 
        string Icon, 
        int Active, 
        int Total, 
        string LastIssued, 
        string LatestReason, 
        DateTimeOffset? LastDate);
}