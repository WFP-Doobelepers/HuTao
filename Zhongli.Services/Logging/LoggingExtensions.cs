using System;
using System.Collections.Generic;
using System.Linq;
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
        public static Color GetColor(this LogType logType)
        {
            return logType switch
            {
                LogType.Created => Color.Blue,
                LogType.Deleted => Color.Red,
                LogType.Updated => Color.Purple,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(logType), logType, "Invalid log type.")
            };
        }

        public static EmbedBuilder AddAttachments(this EmbedBuilder embed, MessageLog log)
        {
            var attachment = log.Attachments.FirstOrDefault();
            if (attachment is null) return embed;

            var url = log.LogType == LogType.Deleted
                ? attachment.ProxyUrl
                : attachment.Url;

            embed.WithImageUrl(url);

            var extra = log.Attachments.Skip(1).ToList();
            return embed.AddAttachments(extra);
        }

        public static string ChannelMentionMarkdown(this IChannelEntity channel)
            => $"{channel.MentionChannel()} ({channel.ChannelId})";

        public static string GetTitle(this ILog log)
        {
            var title = log switch
            {
                MessageLog  => nameof(MessageLog),
                ReactionLog => nameof(ReactionLog),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(log), log, "Invalid log type.")
            };

            return $"{title.Replace("Log", string.Empty)} {log.LogType}";
        }

        public static string JumpUrlMarkdown(this IMessageEntity message)
            => $"[Jump]({message.JumpUrl()}) ({message.MessageId})";

        private static EmbedBuilder AddAttachments(this EmbedBuilder embed, IReadOnlyCollection<Attachment> attachments)
        {
            if (!attachments.Any()) return embed;

            var content = attachments.Select(
                (a, i) => $"[{i}. {a.Width}x{a.Height} {a.Size.Bytes().Humanize()}]({a.Url})");

            return embed.AddField("Other Attachments", string.Join(Environment.NewLine, content));
        }
    }
}