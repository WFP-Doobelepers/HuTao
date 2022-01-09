using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Zhongli.Data.Models.Discord.Message;
using Embed = Zhongli.Data.Models.Discord.Message.Embed;

namespace Zhongli.Services.Utilities;

[Flags]
public enum AuthorOptions
{
    None = 0,
    IncludeId = 1 << 0,
    UseFooter = 1 << 1,
    UseThumbnail = 1 << 2,
    Requested = 1 << 3
}

public static class EmbedBuilderExtensions
{
    public delegate (string Title, StringBuilder Value) EntityViewerDelegate<in T>(T entity);

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

    public static EmbedBuilder AddItemsIntoFields<T>(this EmbedBuilder builder, string title,
        IEnumerable<T> items, Func<T, string> selector, string? separator = null) =>
        builder.AddItemsIntoFields(title, items.Select(selector), separator);

    public static EmbedBuilder AddItemsIntoFields<T>(this EmbedBuilder builder, string title,
        IEnumerable<T> items, Func<T, int, string> selector, string? separator = null) =>
        builder.AddItemsIntoFields(title, items.Select(selector), separator);

    public static EmbedBuilder AddItemsIntoFields(this EmbedBuilder builder, string title,
        IEnumerable<string> items, string? separator = null)
    {
        var splitLines = SplitItemsIntoChunks(items, separator: separator).ToArray();
        return builder.AddIntoFields(title, splitLines);
    }

    public static EmbedBuilder ToBuilder(this Embed embed, bool useProxy = false) => new()
    {
        Author       = embed.Author?.ToBuilder(useProxy),
        Color        = embed.Color,
        Description  = embed.Description,
        Fields       = embed.Fields.Select(e => e.ToBuilder()).ToList(),
        Footer       = embed.Footer?.ToBuilder(useProxy),
        Timestamp    = embed.Timestamp,
        Title        = embed.Title,
        Url          = embed.Url,
        ImageUrl     = useProxy ? embed.Image?.ProxyUrl : embed.Image?.Url,
        ThumbnailUrl = useProxy ? embed.Thumbnail?.ProxyUrl : embed.Thumbnail?.Url
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

    public static EmbedBuilder WithUserAsAuthor(this EmbedBuilder embed, IUser user,
        AuthorOptions authorOptions = AuthorOptions.None, ushort size = 128)
    {
        var username = user.GetFullUsername();
        if (authorOptions.HasFlag(AuthorOptions.Requested))
            username = $"Requested by {username}";

        return embed.WithEntityAsAuthor(user, username, user.GetDefiniteAvatarUrl(size), authorOptions);
    }

    public static IEnumerable<EmbedFieldBuilder> ToEmbedFields<T>(this IEnumerable<T> collection,
        EntityViewerDelegate<T> entityViewer)
        => collection
            .Select(entityViewer.Invoke)
            .Select((e, i) => new EmbedFieldBuilder()
                .WithName($"{i}: {e.Title}")
                .WithValue(e.Value));

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

    private static EmbedAuthorBuilder ToBuilder(this Author author, bool useProxy) => new()
    {
        Name    = author.Name,
        Url     = author.Url,
        IconUrl = useProxy ? author.ProxyIconUrl : author.IconUrl
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

    private static EmbedBuilder WithEntityAsAuthor(this EmbedBuilder embed, IEntity<ulong> entity,
        string name, string? iconUrl, AuthorOptions authorOptions)
    {
        if (authorOptions.HasFlag(AuthorOptions.IncludeId))
            name += $" ({entity.Id})";

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

    private static EmbedFooterBuilder ToBuilder(this Footer footer, bool useProxy) => new()
    {
        Text    = footer.Text,
        IconUrl = useProxy ? footer.ProxyUrl : footer.IconUrl
    };

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
}