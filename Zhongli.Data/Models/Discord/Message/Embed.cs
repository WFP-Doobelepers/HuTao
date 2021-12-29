using System;
using System.Diagnostics.CodeAnalysis;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Embed
{
    protected Embed() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public Embed(IEmbed embed)
    {
        Type        = embed.Type;
        Description = embed.Description;
        Title       = embed.Title;
        Url         = embed.Url;
        Thumbnail   = embed.Thumbnail;
        Color       = embed.Color?.RawValue;
    }

    public Guid Id { get; set; }

    public EmbedType Type { get; set; }

    public string? Description { get; set; }

    public string? Title { get; set; }

    public string? Url { get; set; }

    public virtual Thumbnail? Thumbnail { get; set; }

    public uint? Color { get; set; }
}