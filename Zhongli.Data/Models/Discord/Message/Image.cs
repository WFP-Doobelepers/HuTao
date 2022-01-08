using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Image : IImage
{
    protected Image() { }

    public Image(EmbedImage image)
    {
        Height   = image.Height;
        Width    = image.Width;
        ProxyUrl = image.ProxyUrl;
        Url      = image.Url;
    }

    public Guid Id { get; set; }

    public int? Height { get; set; }

    public int? Width { get; set; }

    public string ProxyUrl { get; set; }

    public string Url { get; set; }

    public static implicit operator Image(EmbedImage image) => new(image);
}