using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;

namespace HuTao.Data.Models.Discord.Message.Embeds;

public class Embed : IEquatable<Embed>
{
    protected Embed() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public Embed(IEmbed embed)
    {
        Author      = embed.Author;
        Timestamp   = embed.Timestamp?.ToUniversalTime();
        Type        = embed.Type;
        Footer      = embed.Footer;
        Image       = embed.Image;
        Description = embed.Description;
        Title       = embed.Title;
        Url         = embed.Url;
        Thumbnail   = embed.Thumbnail;
        Color       = embed.Color?.RawValue;
        Video       = embed.Video;
        Fields      = embed.Fields.Select(e => new Field(e)).ToList();
    }

    public Guid Id { get; init; }

    /// <inheritdoc cref="IEmbed.Author" />
    public virtual Author? Author { get; init; }

    /// <inheritdoc cref="IEmbed.Timestamp" />
    public DateTimeOffset? Timestamp { get; init; }

    /// <inheritdoc cref="IEmbed.Type" />
    public EmbedType Type { get; init; }

    /// <inheritdoc cref="IEmbed.Footer" />
    public virtual Footer? Footer { get; init; }

    /// <inheritdoc cref="IEmbed.Fields" />
    public virtual ICollection<Field> Fields { get; init; } = new List<Field>();

    /// <inheritdoc cref="IEmbed.Image" />
    public virtual Image? Image { get; init; }

    /// <inheritdoc cref="IEmbed.Description" />
    public string? Description { get; init; }

    /// <inheritdoc cref="IEmbed.Title" />
    public string? Title { get; init; }

    /// <inheritdoc cref="IEmbed.Url" />
    public string? Url { get; init; }

    /// <inheritdoc cref="IEmbed.Thumbnail" />
    public virtual Thumbnail? Thumbnail { get; init; }

    /// <inheritdoc cref="IEmbed.Color" />
    public uint? Color { get; init; }

    /// <inheritdoc cref="IEmbed.Video" />
    public virtual Video? Video { get; init; }

    /// <inheritdoc />
    public bool Equals(Embed? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Author == other.Author
            && Timestamp == other.Timestamp
            && Type == other.Type
            && Footer == other.Footer
            && Fields.SequenceEqual(other.Fields)
            && Image == other.Image
            && Description == other.Description
            && Title == other.Title
            && Url == other.Url
            && Thumbnail == other.Thumbnail
            && Color == other.Color
            && Video == other.Video;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is Embed other && Equals(other));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        hashCode.Add(Author);
        hashCode.Add(Timestamp);
        hashCode.Add(Type);
        hashCode.Add(Footer);
        hashCode.Add(Fields);
        hashCode.Add(Image);
        hashCode.Add(Description);
        hashCode.Add(Title);
        hashCode.Add(Url);
        hashCode.Add(Thumbnail);
        hashCode.Add(Color);
        hashCode.Add(Video);

        return hashCode.ToHashCode();
    }

    public static bool operator ==(Embed? left, Embed? right) => Equals(left, right);

    public static bool operator !=(Embed? left, Embed? right) => !Equals(left, right);
}