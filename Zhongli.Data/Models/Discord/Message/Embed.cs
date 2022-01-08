using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Embed
{
    protected Embed() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public Embed(IEmbed embed)
    {
        Author      = embed.Author;
        Timestamp   = embed.Timestamp;
        Type        = embed.Type;
        Footer      = embed.Footer;
        Image       = embed.Image;
        Description = embed.Description;
        Title       = embed.Title;
        Url         = embed.Url;
        Thumbnail   = embed.Thumbnail;
        Color       = embed.Color?.RawValue;
        Video       = embed.Video;

        Fields = embed.Fields.Select(e => new Field(e)).ToList();
    }

    public Guid Id { get; set; }

    /// <inheritdoc cref="IEmbed.Author" />
    public virtual Author? Author { get; set; }

    /// <inheritdoc cref="IEmbed.Timestamp" />
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc cref="IEmbed.Type" />
    public EmbedType Type { get; set; }

    /// <inheritdoc cref="IEmbed.Footer" />
    public virtual Footer? Footer { get; set; }

    /// <inheritdoc cref="IEmbed.Fields" />
    public virtual ICollection<Field> Fields { get; set; }

    /// <inheritdoc cref="IEmbed.Image" />
    public virtual Image? Image { get; set; }

    /// <inheritdoc cref="IEmbed.Description" />
    public string? Description { get; set; }

    /// <inheritdoc cref="IEmbed.Title" />
    public string? Title { get; set; }

    /// <inheritdoc cref="IEmbed.Url" />
    public string? Url { get; set; }

    /// <inheritdoc cref="IEmbed.Thumbnail" />
    public virtual Thumbnail? Thumbnail { get; set; }

    /// <inheritdoc cref="IEmbed.Color" />
    public uint? Color { get; set; }

    /// <inheritdoc cref="IEmbed.Video" />
    public virtual Video? Video { get; set; }
}