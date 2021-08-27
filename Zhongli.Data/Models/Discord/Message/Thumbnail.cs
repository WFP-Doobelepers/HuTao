using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message
{
    public class Thumbnail : IImage
    {
        protected Thumbnail() { }

        public Thumbnail(EmbedThumbnail thumbnail)
        {
            Url      = thumbnail.Url;
            ProxyUrl = thumbnail.ProxyUrl;
            Height   = thumbnail.Height;
            Width    = thumbnail.Width;
        }

        public Guid Id { get; set; }

        public int? Height { get; set; }

        public int? Width { get; set; }

        public string ProxyUrl { get; set; }

        public string Url { get; set; }

        public static implicit operator Thumbnail(EmbedThumbnail thumbnail) => new(thumbnail);
    }
}