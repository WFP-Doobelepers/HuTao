using System;
using Discord;

namespace HuTao.Data.Models.Discord.Message.Embeds;

public class Thumbnail : IImage, IEquatable<Thumbnail>
{
    protected Thumbnail() { }

    public Thumbnail(EmbedThumbnail thumbnail)
    {
        Height   = thumbnail.Height;
        ProxyUrl = thumbnail.ProxyUrl;
        Width    = thumbnail.Width;
        Url      = thumbnail.Url;
    }

    public Thumbnail(IImage thumbnail)
    {
        Height   = thumbnail.Height;
        ProxyUrl = thumbnail.ProxyUrl;
        Width    = thumbnail.Width;
        Url      = thumbnail.Url;
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
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is Thumbnail other && Equals(other));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Height, Width, ProxyUrl, Url);

    public static bool operator ==(Thumbnail? left, Thumbnail? right) => Equals(left, right);

    public static bool operator !=(Thumbnail? left, Thumbnail? right) => !Equals(left, right);

    public static implicit operator Thumbnail(EmbedThumbnail thumbnail) => new(thumbnail);

    public static implicit operator Thumbnail(Image image) => new(image);
}