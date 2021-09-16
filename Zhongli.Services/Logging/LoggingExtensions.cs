using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Humanizer;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Data.Models.Logging;
using Attachment = Zhongli.Data.Models.Discord.Message.Attachment;

namespace Zhongli.Services.Logging
{
    public static class LoggingExtensions
    {
        public static EmbedBuilder AddImages(this EmbedBuilder embed, MessageLog log)
        {
            var images = log.GetImages().ToList();

            var image = images.FirstOrDefault();
            if (image is null) return embed;

            return embed
                .WithImageUrl(image.ProxyUrl)
                .AddOtherImages(images.Skip(1));
        }

        public static string ChannelMentionMarkdown(this IChannelEntity channel)
            => $"{channel.MentionChannel()} ({channel.ChannelId})";

        public static string GetTitle(this ILog log)
        {
            var title = log switch
            {
                IReactionEntity => "Reaction",
                IMessageEntity  => "Message",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(log), log, "Invalid log type.")
            };

            return $"{title.Replace("Log", string.Empty)}";
        }

        public static string JumpUrlMarkdown(this IMessageEntity message)
            => $"[Jump]({message.JumpUrl()}) ({message.MessageId}) from {message.MentionChannel()}";

        private static EmbedBuilder AddOtherImages(this EmbedBuilder embed, IEnumerable<IImage> images)
        {
            var content = images.ToList().GetImageUrls();
            return content is null ? embed : embed.AddField("Other Images", content.ToString());
        }

        private static IEnumerable<IImage> GetImages(this MessageLog log)
        {
            var attachments = log.Attachments.Cast<IImage>();
            var thumbnails = log.Embeds
                .Select(e => e.Thumbnail)
                .OfType<Thumbnail>();

            return attachments.Concat(thumbnails);
        }

        private static StringBuilder? GetImageUrls(this IReadOnlyCollection<IImage> images)
        {
            if (!images.Any()) return null;

            static string Attachment(Attachment a, int i) => $"{Image(a, i)} {a.Size.Bytes().Humanize()}";
            static string Image(IImage a, int i) => $"[{i}. {a.Width}x{a.Height}px]({a.Url}) [Proxy]({a.ProxyUrl})";

            var enumerable = images.ToList();
            var attachments = enumerable.OfType<Attachment>().Select(Attachment);
            var thumbnails = enumerable.OfType<Thumbnail>().Select(Image);

            return new StringBuilder()
                .AppendJoin(Environment.NewLine, attachments)
                .AppendJoin(Environment.NewLine, thumbnails);
        }
    }
}