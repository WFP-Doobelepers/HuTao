using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Video
{
    protected Video() { }

    public Video(EmbedVideo video)
    {
        Height = video.Height;
        Width  = video.Width;
        Url    = video.Url;
    }

    public Guid Id { get; set; }

    /// <inheritdoc cref="EmbedVideo.Height" />
    public int? Height { get; set; }

    /// <inheritdoc cref="EmbedVideo.Width" />
    public int? Width { get; set; }

    /// <inheritdoc cref="EmbedVideo.Url" />
    public string Url { get; set; }

    public static implicit operator Video(EmbedVideo video) => new(video);
}