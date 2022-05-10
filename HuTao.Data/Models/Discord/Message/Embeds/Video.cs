using System;
using Discord;

namespace HuTao.Data.Models.Discord.Message.Embeds;

public class Video : IEquatable<Video>
{
    protected Video() { }

    public Video(EmbedVideo video)
    {
        Height = video.Height;
        Width  = video.Width;
        Url    = video.Url;
    }

    public Guid Id { get; init; }

    /// <inheritdoc cref="EmbedVideo.Height" />
    public int? Height { get; init; }

    /// <inheritdoc cref="EmbedVideo.Width" />
    public int? Width { get; init; }

    /// <inheritdoc cref="EmbedVideo.Url" />
    public string Url { get; init; } = null!;

    /// <inheritdoc />
    public bool Equals(Video? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Height == other.Height && Width == other.Width && Url == other.Url;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is Video other && Equals(other));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Height, Width, Url);

    public static bool operator ==(Video? left, Video? right) => Equals(left, right);

    public static bool operator !=(Video? left, Video? right) => !Equals(left, right);

    public static implicit operator Video(EmbedVideo video) => new(video);
}