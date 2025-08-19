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
        var baseWidth = 600;
        var headerHeight = (int) (28 * scale);
        var rowHeight = (int) (50 * scale);
        var iconSize = (int) (28 * scale);
        var iconGap = (int) (12 * scale);

        var reprimandData = GetReprimandData(user, category);

        var calculatedHeight = headerHeight + rowHeight * reprimandData.Length;
        var width = (int) (baseWidth * scale);
        var height = calculatedHeight;

        var backgroundColor = ImageSharpColor.Transparent;
        var headerColor = ImageSharpColor.ParseHex("5865F2");
        var rowEvenColor = ImageSharpColor.FromRgba(47, 49, 54, 200);
        var rowOddColor = ImageSharpColor.FromRgba(54, 57, 63, 200);
        var textColor = ImageSharpColor.White;
        var textSecondary = ImageSharpColor.ParseHex("B9BBBE");
        var textDark = ImageSharpColor.ParseHex("72767D");
        var iconColor = ImageSharpColor.ParseHex("B9BBBE");

        FontFamily textFont;
        FontFamily titleFontFamily;
        FontFamily iconFont;

        try
        {
            textFont        = SystemFonts.Get("JetBrainsMono NF");
            titleFontFamily = SystemFonts.Get("Segoe UI");
            iconFont        = SystemFonts.Get("Font Awesome 7 Free Solid");
        }
        catch
        {
            textFont        = SystemFonts.Get("Consolas");
            titleFontFamily = SystemFonts.Get("Arial");
            iconFont        = SystemFonts.Get("Segoe UI");
        }

        var headerFont = titleFontFamily.CreateFont(12 * scale, FontStyle.Bold);
        var dataFont = textFont.CreateFont(14 * scale, FontStyle.Regular);
        var smallFont = textFont.CreateFont(12 * scale, FontStyle.Regular);
        var reasonFont = textFont.CreateFont(8 * scale, FontStyle.Regular);
        var iconFontSize = iconFont.CreateFont(20 * scale, FontStyle.Regular);

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
            var iconX = 20 * scale;
            var typeX = iconX + iconSize + iconGap;
            var countX = 180 * scale;
            var actionX = 280 * scale;
            var reasonX = 420 * scale;

            ctx.DrawText("Type", headerFont, textColor, new PointF(typeX, headY));
            ctx.DrawText("Count", headerFont, textColor, new PointF(countX, headY));
            ctx.DrawText("Last Action", headerFont, textColor, new PointF(actionX, headY));
            ctx.DrawText("Latest Reason", headerFont, textColor, new PointF(reasonX, headY));

            for (var i = 0; i < reprimandData.Length; i++)
            {
                var data = reprimandData[i];
                float rowY = headerHeight + i * rowHeight;

                var textY = rowY + (rowHeight - dataFont.Size) / 2f;
                var iconY = rowY + (rowHeight - iconFontSize.Size) / 2f;

                ctx.DrawText(data.Icon, iconFontSize, iconColor,
                    new PointF(iconX, iconY));

                ctx.DrawText(data.Label, dataFont, textColor,
                    new PointF(typeX, textY));

                var activeText = data.Active.ToString();
                var totalText = data.Total.ToString();
                var activeTextColor = data.Active > 0 ? textColor : textSecondary;

                ctx.DrawText(activeText, dataFont, activeTextColor,
                    new PointF(countX, textY));

                var activeWidth = activeText.Length * 8 * scale;
                ctx.DrawText("/", dataFont, textDark,
                    new PointF(countX + activeWidth + 2 * scale, textY));

                var separatorWidth = 6 * scale;
                ctx.DrawText(totalText, dataFont, textDark,
                    new PointF(countX + activeWidth + separatorWidth + 4 * scale, textY));

                ctx.DrawText(data.LastIssued, smallFont, textSecondary,
                    new PointF(actionX, textY));

                var reasonText = data.LatestReason.Length > 40 ? data.LatestReason[..37] + "..." : data.LatestReason;
                ctx.DrawText(reasonText, reasonFont, textSecondary,
                    new PointF(reasonX, textY));
            }
        });

        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        sw.Stop();
        Debug.WriteLine($"Generated history image in {sw.Elapsed.Humanize()}");

        return stream;
    }

    private static (string Label, string Icon, int Active, int Total, string LastIssued, string LatestReason)
        GetReprimandInfo<T>(string label, string icon, GuildUserEntity user, ModerationCategory? category) where T : Reprimand
    {
        var count = typeof(T) == typeof(Warning)
            ? user.WarningCount(category)
            : user.HistoryCount<T>(category);

        var latest = user.Reprimands<T>(category).OrderByDescending(r => r.Action?.Date).FirstOrDefault();

        var lastIssued = latest?.Action?.Date.Humanize() ?? "-";
        var latestReason = latest?.GetLatestReason() ?? "-";

        return (label, icon, (int) count.Active, (int) count.Total, lastIssued, latestReason);
    }

    private static (string Label, string Icon, int Active, int Total, string LastIssued, string LatestReason)[] GetReprimandData(
        GuildUserEntity user, ModerationCategory? category)
    {
        var rules = category?.Logging?.SummaryReprimands
            ?? user.Guild.ModerationRules?.Logging?.SummaryReprimands
            ?? LogReprimandType.All;

        var data = new List<(string Label, string Icon, int Active, int Total, string LastIssued, string LatestReason)>();

        if (rules.HasFlag(LogReprimandType.Warning))
            data.Add(GetReprimandInfo<Warning>("Warning", "\uf071", user, category));

        if (rules.HasFlag(LogReprimandType.Notice))
            data.Add(GetReprimandInfo<Notice>("Notice", "\uf0a1", user, category));

        if (rules.HasFlag(LogReprimandType.Ban))
            data.Add(GetReprimandInfo<Ban>("Ban", "\uf05e", user, category));

        if (rules.HasFlag(LogReprimandType.Kick))
            data.Add(GetReprimandInfo<Kick>("Kick", "\uf54b", user, category));

        if (rules.HasFlag(LogReprimandType.Note))
            data.Add(GetReprimandInfo<Note>("Note", "\uf249", user, category));

        if (rules.HasFlag(LogReprimandType.Mute))
            data.Add(GetReprimandInfo<Mute>("Mute", "\uf131", user, category));

        if (false && rules.HasFlag(LogReprimandType.HardMute))
            data.Add(GetReprimandInfo<HardMute>("Hard Mute", "\uf131", user, category));

        if (true || rules.HasFlag(LogReprimandType.Timeout))
            data.Add(GetReprimandInfo<Timeout>("Timeout", "\uf251", user, category));

        if (rules.HasFlag(LogReprimandType.Censored))
            data.Add(GetReprimandInfo<Censored>("Censored", "\uf070", user, category));

        return data.OrderByDescending(x => x.LastIssued == "-" ? 0 : 1).ToArray();
    }
}