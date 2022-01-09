using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Author : IEquatable<Author>
{
    protected Author() { }

    public Author(EmbedAuthor author)
    {
        Name         = author.Name;
        Url          = author.Url;
        IconUrl      = author.IconUrl;
        ProxyIconUrl = author.ProxyIconUrl;
    }

    public Guid Id { get; set; }

    /// <inheritdoc cref="EmbedAuthor.Name" />
    public string Name { get; init; } = null!;

    /// <inheritdoc cref="EmbedAuthor.IconUrl" />
    public string? IconUrl { get; init; }

    /// <inheritdoc cref="EmbedAuthor.ProxyIconUrl" />
    public string? ProxyIconUrl { get; init; }

    /// <inheritdoc cref="EmbedAuthor.Url" />
    public string? Url { get; init; }

    /// <inheritdoc />
    public bool Equals(Author? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && IconUrl == other.IconUrl && ProxyIconUrl == other.ProxyIconUrl && Url == other.Url;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Author other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, IconUrl, ProxyIconUrl, Url);

    public static implicit operator Author(EmbedAuthor author) => new(author);

    public static bool operator ==(Author? left, Author? right) => Equals(left, right);

    public static bool operator !=(Author? left, Author? right) => !Equals(left, right);
}