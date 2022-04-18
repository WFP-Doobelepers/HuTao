using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            _ => throw new ArgumentOutOfRangeException(
                nameof(log), log, "Invalid log type.")
        };

        return $"{title.Replace("Log", string.Empty)}";
    }

    public static string JumpUrlMarkdown(this IMessageEntity message)
        => $"[{message.MessageId}]({message.JumpUrl()}) in {message.MentionChannel()}";

    public static async Task<StringBuilder> GetDetailsAsync(this IEnumerable<MessageLog> logs, IDiscordClient client)
    {
        var builder = new StringBuilder();

        foreach (var (message, index) in logs.AsIndexable())
        {
            builder
                .AppendLine($"## Message {index}")
                .Append(message.GetDetails(await client.GetUserAsync(message.UserId)));
        }

        return builder;
    }

    internal static EmbedBuilder WithMessageReference(
        this EmbedBuilder embed, MessageLog log,
        MessageLog? reply, IUser? replyUser) => reply is null
        ? embed.AddField("Referenced Message", log.ReferencedJumpMarkdown(), true)
        : embed.AddField($"Reply to {replyUser}", new StringBuilder()
            .AppendLine($"{log.ReferencedJumpMarkdown()} by {reply.MentionUser()}")
            .AppendLine(reply.Content.Truncate(512)));

    private static IEnumerable<IImage> GetImages(this MessageLog log)
    {
        var attachments = log.Attachments.Cast<IImage>();
        var thumbnails = log.Embeds
            .Select(e => e.Thumbnail)
            .OfType<Thumbnail>();

        return attachments.Concat(thumbnails);
    }

    private static string ReferencedJumpMarkdown(this MessageLog message)
        => $"[{message.MessageId}]({message.ReferencedJumpUrl()}) in {message.MentionChannel()}";

    private static string ReferencedJumpUrl(this MessageLog entity)
        => $"https://discord.com/channels/{entity.GuildId}/{entity.ChannelId}/{entity.ReferencedMessageId}";

    private static StringBuilder AppendImageUrls(this StringBuilder builder, IReadOnlyCollection<IImage> images)
    {
        if (!images.Any()) return builder;

        foreach (var image in images)
        {
            builder.AppendLine(image switch
            {
                Attachment a => ((IAttachment) a).GetDetails(),
                _            => image.GetDetails()
            });
        }

        return builder;
    }

    private static StringBuilder GetDetails(this MessageLog log, IUser user)
    {
        var builder = new StringBuilder()
            .AppendLine("### Details")
            .AppendLine($"- User: {user} {log.MentionUser()}")
            .AppendLine($"- ID: [{log.Id}]({log.JumpUrl()})")
            .AppendLine($"- Channel: [{log.MentionChannel()}]")
            .AppendLine($"- Date: {log.LogDate}");

        if (log.EditedTimestamp is not null)
            builder.AppendLine($"- Edited: {log.EditedTimestamp}");

        if (log.ReferencedMessageId is not null)
            builder.AppendLine($"- Reply to: [{log.ReferencedMessageId}]({log.ReferencedJumpUrl()})");

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