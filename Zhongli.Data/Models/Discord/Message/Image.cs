using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Image : IImage, IEquatable<Image>
{
    protected Image() { }

    public Image(EmbedImage image)
    {
        Height   = image.Height;
        Width    = image.Width;
        ProxyUrl = image.ProxyUrl;
        Url      = image.Url;
    }

    public Guid Id { get; init; }

    /// <inheritdoc />
    public bool Equals(Image? other)
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
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Image other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Height, Width, ProxyUrl, Url);

    public static bool operator ==(Image? left, Image? right) => Equals(left, right);

    public static bool operator !=(Image? left, Image? right) => !Equals(left, right);

    public static implicit operator Image(EmbedImage image) => new(image);
}