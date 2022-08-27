using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Humanizer;
using HuTao.Data.Models.Discord.Message;
using HuTao.Data.Models.Discord.Message.Embeds;
using HuTao.Data.Models.Logging;
using static HuTao.Services.Utilities.EmbedBuilderOptions;
using Embed = HuTao.Data.Models.Discord.Message.Embeds.Embed;

namespace HuTao.Services.Utilities;

[Flags]
public enum AuthorOptions
{
    None = 0,
    IncludeId = 1 << 0,
    UseFooter = 1 << 1,
    UseThumbnail = 1 << 2,
    Requested = 1 << 3
}

[Flags]
public enum EmbedBuilderOptions
{
    None = 0,
    UseProxy = 1 << 0,
    ReplaceTimestamps = 1 << 1,
    ReplaceAnimations = 1 << 2,
    EnlargeThumbnails = 1 << 3,
    UploadAttachments = 1 << 4
}

public static class EmbedBuilderExtensions
{
    private static readonly Regex Giphy = new(@"giphy\.com/media/(?<id>\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    private static readonly Regex Tenor = new(@"tenor\.com/(?<id>[\w-]+?)A+(\w+)/(?<n>[\w-]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static bool IsViewable(this Embed embed)
        => embed.Length() > 0
            || embed.Image is not null
            || embed.Thumbnail is not null;

    public static EmbedAuthorBuilder WithGuildAsAuthor(this EmbedAuthorBuilder embed, IGuild guild,
        AuthorOptions authorOptions = AuthorOptions.None)
    {
        var name = guild.Name;
        if (authorOptions.HasFlag(AuthorOptions.Requested))
            name = $"Requested from {name}";

        return embed.WithEntityAsAuthor(guild, name, guild.IconUrl, authorOptions);
    }

    public static EmbedBuilder AddContent(this EmbedBuilder embed, IMessage message)
        => embed.AddContent(message.Content);

    public static EmbedBuilder AddContent(this EmbedBuilder embed, string? content)
        => string.IsNullOrWhiteSpace(content) ? embed : embed.WithDescription(content);

    public static EmbedBuilder AddItemsIntoFields<T>(this EmbedBuilder builder, string title,
        IEnumerable<T>? items, Func<T, string> selector, string? separator = null) =>
        builder.AddItemsIntoFields(title, items?.Select(selector), separator);

    public static EmbedBuilder AddItemsIntoFields<T>(this EmbedBuilder builder, string title,
        IEnumerable<T>? items, Func<T, int, string> selector, string? separator = null) =>
        builder.AddItemsIntoFields(title, items?.Select(selector), separator);

    public static EmbedBuilder AddItemsIntoFields(this EmbedBuilder builder, string title,
        IEnumerable<string>? items, string? separator = null)
    {
        if (items is null) return builder;
        var splitLines = SplitItemsIntoChunks(items, separator: separator).ToArray();
        return builder.AddIntoFields(title, splitLines);
    }

    public static EmbedBuilder ToBuilder(this Embed embed, EmbedBuilderOptions options = None) => new()
    {
        Author       = embed.Author?.ToBuilder(options),
        Color        = embed.Color,
        Description  = embed.Description,
        Fields       = embed.Fields.Select(e => e.ToBuilder()).ToList(),
        Footer       = embed.Footer?.ToBuilder(options),
        Timestamp    = options.HasFlag(ReplaceTimestamps) ? DateTimeOffset.UtcNow : embed.Timestamp,
        Title        = embed.Title,
        Url          = embed.Url,
        ImageUrl     = embed.GetImages(options).Image,
        ThumbnailUrl = embed.GetImages(options).Thumbnail
    };

    public static EmbedBuilder WithGuildAsAuthor(this EmbedBuilder embed, IGuild? guild,
        AuthorOptions authorOptions = AuthorOptions.None)
    {
        if (guild is null) return embed;

        var name = guild.Name;
        if (authorOptions.HasFlag(AuthorOptions.Requested))
            name = $"Requested from {name}";

        return embed.WithEntityAsAuthor(guild, name, guild.IconUrl, authorOptions);
    }

    public static EmbedBuilder WithUserAsAuthor(this EmbedBuilder embed, IUser? user,
        AuthorOptions authorOptions = AuthorOptions.None, ushort size = 128)
    {
        var username = user?.GetFullUsername() ?? "Unknown";
        if (authorOptions.HasFlag(AuthorOptions.Requested))
            username = $"Requested by {username}";

        return embed.WithEntityAsAuthor(user, username, user?.GetDefiniteAvatarUrl(size), authorOptions);
    }

    public static IEnumerable<EmbedBuilder> ToEmbedBuilders(
        this IReadOnlyCollection<IAttachment> attachments,
        EmbedBuilderOptions options)
    {
        if (!attachments.Any()) return Enumerable.Empty<EmbedBuilder>();

        var description = string.Join(Environment.NewLine, attachments.Select(GetDetails));
        var footer = string.Join(Environment.NewLine, attachments.Select(Footer));
        var url = options.HasFlag(UseProxy) ? attachments.First().ProxyUrl : attachments.First().Url;

        return attachments.Select(a => new EmbedBuilder()
            .WithUrl(url).WithDescription(description)
            .WithFooter(footer.Truncate(EmbedBuilder.MaxDescriptionLength))
            .WithImageUrl(options.HasFlag(UploadAttachments)
                ? $"attachment://{a.Filename}"
                : options.HasFlag(UseProxy)
                    ? a.ProxyUrl
                    : a.Url));

        static string Footer(IAttachment i)
            => $"{i.Filename.Truncate(EmbedBuilder.MaxTitleLength)} {i.Width}x{i.Height}px {i.Size.Bytes().Humanize()}";
    }

    public static IEnumerable<EmbedBuilder> ToEmbedBuilders(this MessageLog message, EmbedBuilderOptions options)
        => message.Embeds
            .Select(e => e.ToBuilder(options))
            .Concat(message.Attachments.ToList().ToEmbedBuilders(options));

    public static IEnumerable<EmbedBuilder> ToEmbedBuilders(this IMessage message, EmbedBuilderOptions options)
        => message.Embeds
            .Select(e => new Embed(e).ToBuilder(options))
            .Concat(message.Attachments.ToEmbedBuilders(options));

    public static int Length(this Embed embed)
    {
        return
            L(embed.Title) +
            L(embed.Author?.Name) +
            L(embed.Description) +
            L(embed.Footer?.Text) +
            embed.Fields.Sum(f =>
                L(f.Name) +
                L(f.Value));

        int L(string? s) => s?.Length ?? 0;
    }

    public static string GetDetails(this IAttachment a)
        => $"**[{a.Width}x{a.Height}px]({a.Url})** ([Proxy]({a.ProxyUrl})) {a.Size.Bytes().Humanize()}";

    public static string GetDetails(this IImage a)
        => $"**[{a.Width}x{a.Height}px]({a.Url})** ([Proxy]({a.ProxyUrl}))";

    private static bool TryGiphy(string url, out string gif)
    {
        var match = Giphy.Match(url);
        gif = @$"https://media.giphy.com/media/{match.Groups["id"]}/giphy.gif";

        return match.Success;
    }

    private static bool TryTenor(string url, out string gif)
    {
        var match = Tenor.Match(url);
        gif = @$"https://media.tenor.com/{match.Groups["id"]}AAAAC/{match.Groups["n"]}.gif";

        return match.Success;
    }

    private static EmbedAuthorBuilder ToBuilder(this Author author, EmbedBuilderOptions options) => new()
    {
        Name    = author.Name,
        Url     = author.Url,
        IconUrl = options.HasFlag(UseProxy) ? author.ProxyIconUrl : author.IconUrl
    };

    private static EmbedAuthorBuilder WithEntityAsAuthor(this EmbedAuthorBuilder embed, IEntity<ulong> entity,
        string name, string iconUrl, AuthorOptions authorOptions)
    {
        if (authorOptions.HasFlag(AuthorOptions.IncludeId))
            name += $" ({entity.Id})";

        return embed.WithName(name).WithIconUrl(iconUrl);
    }

    private static EmbedBuilder AddIntoFields(this EmbedBuilder builder, string title,
        IReadOnlyCollection<string> items)
    {
        if (!items.Any()) return builder;

        builder.AddField(title, items.First());
        foreach (var line in items.Skip(1))
        {
            builder.AddField("\x200b", line);
        }

        return builder;
    }

    private static EmbedBuilder WithEntityAsAuthor(this EmbedBuilder embed, IEntity<ulong>? entity,
        string name, string? iconUrl, AuthorOptions authorOptions)
    {
        if (authorOptions.HasFlag(AuthorOptions.IncludeId))
            name += $" ({entity?.Id})";

        if (authorOptions.HasFlag(AuthorOptions.UseThumbnail))
            embed.WithThumbnailUrl(iconUrl);

        return authorOptions.HasFlag(AuthorOptions.UseFooter)
            ? embed.WithFooter(name, iconUrl)
            : embed.WithAuthor(name, iconUrl);
    }

    private static EmbedFieldBuilder ToBuilder(this Field field) => new()
    {
        Name     = field.Name,
        Value    = field.Value,
        IsInline = field.Inline
    };

    private static EmbedFooterBuilder ToBuilder(this Footer footer, EmbedBuilderOptions options) => new()
    {
        Text    = footer.Text,
        IconUrl = options.HasFlag(UseProxy) ? footer.ProxyUrl : footer.IconUrl
    };

    private static EmbedImages GetImages(this Embed embed, EmbedBuilderOptions options)
    {
        string? image;
        string? thumbnail;

        if (options.HasFlag(EnlargeThumbnails))
        {
            image     = embed.Image.GetUrl(options) ?? embed.Thumbnail.GetUrl(options);
            thumbnail = image is null ? embed.Thumbnail.GetUrl(options) : null;
        }
        else
        {
            image     = embed.Image.GetUrl(options);
            thumbnail = embed.Thumbnail.GetUrl(options);
        }

        if (options.HasFlag(ReplaceAnimations))
        {
            image     = ReplaceAnimation(image);
            thumbnail = ReplaceAnimation(thumbnail);
        }

        return new EmbedImages(image, thumbnail);
    }

    private static IEnumerable<string> SplitItemsIntoChunks(this IEnumerable<string> items,
        int maxLength = EmbedFieldBuilder.MaxFieldValueLength, string? separator = null)
    {
        var sb = new StringBuilder(0, maxLength);
        var builders = new List<StringBuilder>();

        foreach (var item in items)
        {
            if (sb.Length + (separator ?? Environment.NewLine).Length + item.Length > maxLength)
            {
                builders.Add(sb);
                sb = new StringBuilder(0, maxLength);
            }

            if (separator is null)
                sb.AppendLine(item);
            else
                sb.Append(item).Append(separator);
        }

        builders.Add(sb);

        return builders
            .Where(s => s.Length > 0)
            .Select(s => s.ToString());
    }

    private static string? GetUrl(this IImage? image, EmbedBuilderOptions options)
        => options.HasFlag(UseProxy) ? image?.ProxyUrl : image?.Url;

    private static string? ReplaceAnimation(string? url)
        => string.IsNullOrEmpty(url)     ? null :
            TryTenor(url, out var tenor) ? tenor :
            TryGiphy(url, out var giphy) ? giphy :
                                           url;

    private record EmbedImages(string? Image, string? Thumbnail);
}