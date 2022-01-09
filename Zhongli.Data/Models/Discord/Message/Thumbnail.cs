using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Thumbnail : IImage, IEquatable<Thumbnail>
{
    protected Thumbnail() { }

    public Thumbnail(EmbedThumbnail thumbnail)
    {
        Url      = thumbnail.Url;
        ProxyUrl = thumbnail.ProxyUrl;
        Height   = thumbnail.Height;
        Width    = thumbnail.Width;
    }

    public Guid Id { get; init; }

    /// <inheritdoc />
    public bool Equals(Thumbnail? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Height == other.Height && Width == other.Width && ProxyUrl == other.ProxyUrl && Url == other.Url;
    }

    public int? Height { get; init; }

    public int? Width { get; init; }

    public string ProxyUrl { get; init; } = null!;

    public string Url { get; init; } = null!;

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Thumbnail other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Height, Width, ProxyUrl, Url);

    public static bool operator ==(Thumbnail? left, Thumbnail? right) => Equals(left, right);

    public static bool operator !=(Thumbnail? left, Thumbnail? right) => !Equals(left, right);

    public static implicit operator Thumbnail(EmbedThumbnail thumbnail) => new(thumbnail);
}