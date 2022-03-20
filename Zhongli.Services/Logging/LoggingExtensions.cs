using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Humanizer;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Data.Models.Discord.Message.Embeds;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Utilities;
using Attachment = Zhongli.Data.Models.Discord.Message.Attachment;

namespace Zhongli.Services.Logging;

public static class LoggingExtensions
{
    public static IEnumerable<EmbedBuilder> ToBuilder(this IEnumerable<Attachment> attachments)
    {
        var images = attachments.ToList();
        var description = string.Join(Environment.NewLine, images.Select(Attachment));
        var footer = string.Join(Environment.NewLine, images.Select(Footer));
        var url = images.First().ProxyUrl;

        return images.Select(a => new EmbedBuilder()
            .WithUrl(url).WithFooter(footer)
            .WithDescription(description)
            .WithImageUrl(a.ProxyUrl));

        static string Footer(IAttachment i) => $"{i.Filename} {i.Width}x{i.Height}px {i.Size.Bytes().Humanize()}";
    }

    public static string ChannelMentionMarkdown(this IChannelEntity channel)
        => $"{channel.MentionChannel()} ({channel.ChannelId})";

    public static string GetTitle(this ILog log)
    {
        var title = log switch
        {
            MessageDeleteLog  => "Message",
            MessageLog        => "Message",
            MessagesDeleteLog => "Bulk Message",
            ReactionDeleteLog => "Reaction",
            ReactionLog       => "Reaction",
            UserJoinLog       => "User Join",
            _ => throw new ArgumentOutOfRangeException(
                nameof(log), log, "Invalid log type.")
        };

        return $"{title.Replace("Log", string.Empty)}";
    }

    public static string JumpUrlMarkdown(this IMessageEntity message)
        => $"[Jump]({message.JumpUrl()}) ({message.MessageId}) from {message.MentionChannel()}";

    public static StringBuilder GetDetails(this IEnumerable<MessageLog> logs)
    {
        var builder = new StringBuilder();

        foreach (var (message, index) in logs.AsIndexable())
        {
            builder
                .AppendLine($"## Message {index}")
                .Append(message.GetDetails());
        }

        return builder;
    }

    private static IEnumerable<IImage> GetImages(this MessageLog log)
    {
        var attachments = log.Attachments.Cast<IImage>();
        var thumbnails = log.Embeds
            .Select(e => e.Thumbnail)
            .OfType<Thumbnail>();

        return attachments.Concat(thumbnails);
    }

    private static string Attachment(this Attachment a) => $"{Image(a)} {a.Size.Bytes().Humanize()}";

    private static string Image(IImage a) => $"**[{a.Width}x{a.Height}px]({a.Url})** ([Proxy]({a.ProxyUrl}))";

    private static StringBuilder AppendImageUrls(this StringBuilder builder, IReadOnlyCollection<IImage> images)
    {
        if (!images.Any()) return builder;

        foreach (var image in images)
        {
            builder.AppendLine(image switch
            {
                Attachment a => Attachment(a),
                _            => Image(image)
            });
        }

        return builder;
    }

    private static StringBuilder GetDetails(this MessageLog log)
    {
        var builder = new StringBuilder()
            .AppendLine("### Details")
            .AppendLine($"- User: {log.User.Username} {log.MentionUser()}")
            .AppendLine($"- ID: [{log.Id}]({log.JumpUrl()})")
            .AppendLine($"- Channel: [{log.MentionChannel()}]")
            .AppendLine($"- Date: {log.LogDate}");

        if (log.EditedTimestamp is not null)
            builder.AppendLine($"- Edited: {log.EditedTimestamp}");

        if (!string.IsNullOrWhiteSpace(log.Content))
        {
            builder.AppendLine("### Content");
            foreach (var line in log.Content.Split(Environment.NewLine))
            {
                builder.AppendLine($"> {line}");
            }
        }

        var images = log.GetImages().ToList();
        if (images.Any())
        {
            builder
                .AppendLine("### Images")
                .AppendImageUrls(images);
        }

        return builder;
    }
}