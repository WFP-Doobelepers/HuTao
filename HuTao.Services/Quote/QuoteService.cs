using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Services.Logging;
using HuTao.Services.Utilities;
using Embed = HuTao.Data.Models.Discord.Message.Embeds.Embed;

namespace HuTao.Services.Quote;

public record JumpMessage(ulong GuildId, ulong ChannelId, ulong MessageId, bool Suppressed);

public record QuotedMessage(Context Context, ulong ChannelId, ulong MessageId, ulong UserId)
    : JumpMessage(Context.Guild.Id, ChannelId, MessageId, false);

public interface IQuoteService
{
    Task<MessageComponent?> BuildQuoteAsync(
        Context context,
        IUser requester,
        IEnumerable<JumpMessage> jumpUrls);
}

public class QuoteService(LoggingService logging, HuTaoContext db) : IQuoteService
{
    private const int MaxReplyDepth = 3;
    private const int MaxReplyContentLength = 200;

    public async Task<MessageComponent?> BuildQuoteAsync(
        Context context, IUser requester,
        IEnumerable<JumpMessage> jumpUrls)
    {
        var jumpMessages = jumpUrls
            .Where(jump => !jump.Suppressed)
            .DistinctBy(j => j.MessageId)
            .ToList();

        var quotes = new List<(ContainerBuilder Container, string JumpUrl)>();

        foreach (var jump in jumpMessages)
        {
            var message = await jump.GetMessageAsync(context);
            if (message is not null)
            {
                quotes.Add((await BuildMessageContainer(message), message.GetJumpUrl()));
                continue;
            }

            if (context.User is not IGuildUser guildUser) continue;

            var guild = await db.Guilds.TrackGuildAsync(context.Guild);
            var loggingChannel
                = guild.LoggingRules?.LoggingChannels.FirstOrDefault(l => l.Type is LogType.MessageDeleted);
            if (loggingChannel is null) continue;

            var channel = await context.Guild.GetTextChannelAsync(loggingChannel.ChannelId);
            var permissions = guildUser.GetPermissions(channel);
            if (!permissions.ViewChannel) continue;

            var log = await logging.GetLatestMessage(jump.GuildId, jump.ChannelId, jump.MessageId);
            if (log is null || log.Guild.Id != context.Guild.Id) continue;

            quotes.Add((await BuildLogContainer(log), log.GetJumpUrl()));
        }

        if (quotes.Count == 0)
            return null;

        var builder = new ComponentBuilderV2();
        foreach (var (container, jumpUrl) in quotes)
        {
            container
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithSection(new SectionBuilder()
                    .WithTextDisplay($"-# Requested by {requester.Mention}")
                    .WithAccessory(ButtonBuilder.CreateLinkButton("Jump", jumpUrl)));

            builder.WithContainer(container);
        }

        return builder.Build();
    }

    private static async Task<ContainerBuilder> BuildMessageContainer(IMessage message)
    {
        var container = new ContainerBuilder();

        await AppendReplyChain(container, message);

        var sb = new StringBuilder();
        sb.AppendLine(FormatHeader(message.Author.Mention, message.Timestamp));

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            var content = message.Content;
            if (content.Length > 2500) content = $"{content[..2500]}…";
            sb.Append(content);
        }

        container.WithTextDisplay(sb.ToString().TrimEnd());

        foreach (var embed in message.Embeds)
            AppendRenderedEmbed(container, embed.Author?.Name, embed.Author?.Url,
                embed.Title, embed.Url, embed.Description,
                embed.Fields.Select(f => (f.Name, f.Value)),
                embed.Footer?.Text);

        AppendComponentsV2Text(container, message.Components);

        AppendMedia(container, message.Attachments, message.Embeds);
        AppendFileAttachments(container, message.Attachments);

        return container;
    }

    private async Task<ContainerBuilder> BuildLogContainer(MessageLog log)
    {
        var container = new ContainerBuilder();

        await AppendLogReplyChain(container, log);

        var sb = new StringBuilder();
        sb.Append(FormatHeader($"<@{log.UserId}>", log.Timestamp));
        sb.AppendLine(" · -# *(deleted)*");

        if (!string.IsNullOrWhiteSpace(log.Content))
        {
            var content = log.Content;
            if (content.Length > 2500) content = $"{content[..2500]}…";
            sb.Append(content);
        }

        container.WithTextDisplay(sb.ToString().TrimEnd());

        foreach (var embed in log.Embeds)
            AppendRenderedEmbed(container, embed.Author?.Name, embed.Author?.Url,
                embed.Title, embed.Url, embed.Description,
                embed.Fields.Select(f => (f.Name, f.Value)),
                embed.Footer?.Text);

        var media = log.Attachments
            .Where(a => IsImageUrl(a.Url))
            .Take(10)
            .Select(a => new MediaGalleryItemProperties(new UnfurledMediaItemProperties(a.Url)))
            .ToList();

        foreach (var embed in log.Embeds)
        {
            if (media.Count >= 10) break;
            if (embed.Image?.Url is { } url && media.All(m => m.Media.Url != url))
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(url)));
        }

        if (media.Count > 0)
            container.WithMediaGallery(media);

        var files = log.Attachments
            .Where(a => !IsImageUrl(a.Url))
            .Select(a => $"- [{a.Filename}]({a.Url})")
            .Take(8)
            .ToList();

        if (files.Count > 0)
            container.WithTextDisplay(string.Join("\n", files));

        return container;
    }

    private static async Task AppendReplyChain(ContainerBuilder container, IMessage message)
    {
        var replies = new List<IMessage>();
        var current = message;

        while (replies.Count < MaxReplyDepth && current.Reference is not null)
        {
            var reply = (current as IUserMessage)?.ReferencedMessage
                        ?? await current.Channel.GetMessageAsync(current.Reference.MessageId.Value);
            if (reply is null) break;
            replies.Add(reply);
            current = reply;
        }

        for (var i = replies.Count - 1; i >= 0; i--)
        {
            var reply = replies[i];
            var sb = new StringBuilder();
            sb.AppendLine(FormatHeader(reply.Author.Mention, reply.Timestamp));

            var content = ExtractDisplayContent(reply);
            if (!string.IsNullOrWhiteSpace(content))
                sb.Append(content);

            container.WithTextDisplay(sb.ToString().TrimEnd());
            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }
    }

    private async Task AppendLogReplyChain(ContainerBuilder container, MessageLog log)
    {
        var replies = new List<MessageLog>();
        var current = log;

        while (replies.Count < MaxReplyDepth && current.ReferencedMessageId is not null)
        {
            var reply = await logging.GetLatestMessage(
                current.GuildId, current.ChannelId, current.ReferencedMessageId.Value);
            if (reply is null) break;
            replies.Add(reply);
            current = reply;
        }

        for (var i = replies.Count - 1; i >= 0; i--)
        {
            var reply = replies[i];
            var sb = new StringBuilder();
            sb.AppendLine(FormatHeader($"<@{reply.UserId}>", reply.Timestamp));

            var content = ExtractLogContent(reply);
            if (!string.IsNullOrWhiteSpace(content))
                sb.Append(content);

            container.WithTextDisplay(sb.ToString().TrimEnd());
            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }
    }

    private static string ExtractDisplayContent(IMessage message, int maxLength = MaxReplyContentLength)
    {
        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            return message.Content.Length > maxLength
                ? $"{message.Content[..maxLength]}…"
                : message.Content;
        }

        var embedContent = ExtractEmbedContent(maxLength,
            message.Embeds.Select(e => (
                AuthorName: e.Author?.Name,
                Title: (string?) e.Title,
                Description: (string?) e.Description,
                Fields: e.Fields.Select(f => (f.Name, f.Value)),
                FooterText: e.Footer?.Text)));

        return !string.IsNullOrEmpty(embedContent)
            ? embedContent
            : ExtractV2Text(message.Components, maxLength);
    }

    private static string ExtractLogContent(MessageLog log, int maxLength = MaxReplyContentLength)
    {
        if (!string.IsNullOrWhiteSpace(log.Content))
        {
            return log.Content.Length > maxLength
                ? $"{log.Content[..maxLength]}…"
                : log.Content;
        }

        return ExtractEmbedContent(maxLength,
            log.Embeds.Select(e => (
                AuthorName: (string?) e.Author?.Name,
                Title: (string?) e.Title,
                Description: (string?) e.Description,
                Fields: e.Fields.Select(f => (f.Name, f.Value)),
                FooterText: (string?) e.Footer?.Text)));
    }

    private static string ExtractEmbedContent(int maxLength,
        IEnumerable<(string? AuthorName, string? Title, string? Description,
            IEnumerable<(string Name, string Value)> Fields, string? FooterText)> embeds)
    {
        var parts = new List<string>();

        foreach (var embed in embeds)
        {
            if (!string.IsNullOrWhiteSpace(embed.AuthorName))
            {
                var display = embed.AuthorName.StartsWith('@') ? embed.AuthorName : $"@{embed.AuthorName}";
                parts.Add(display);
            }

            if (!string.IsNullOrWhiteSpace(embed.Title))
                parts.Add($"**{embed.Title}**");

            if (!string.IsNullOrWhiteSpace(embed.Description))
                parts.Add(embed.Description);

            foreach (var (name, value) in embed.Fields)
                parts.Add($"**{name}:** {value}");

            if (!string.IsNullOrWhiteSpace(embed.FooterText))
                parts.Add($"-# {embed.FooterText}");
        }

        if (parts.Count == 0) return "";

        var combined = string.Join("\n", parts);
        return combined.Length > maxLength
            ? $"{combined[..maxLength]}…"
            : combined;
    }

    private static void AppendComponentsV2Text(
        ContainerBuilder container,
        IReadOnlyCollection<IMessageComponent> components)
    {
        var parts = new List<string>();
        ExtractV2TextParts(components, parts);

        if (parts.Count == 0) return;

        var combined = string.Join("\n", parts);
        if (combined.Length > 3000) combined = $"{combined[..3000]}…";
        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        container.WithTextDisplay(combined);
    }

    private static string ExtractV2Text(
        IReadOnlyCollection<IMessageComponent> components,
        int maxLength = MaxReplyContentLength)
    {
        var parts = new List<string>();
        ExtractV2TextParts(components, parts);

        if (parts.Count == 0) return "";

        var combined = string.Join("\n", parts);
        return combined.Length > maxLength ? $"{combined[..maxLength]}…" : combined;
    }

    private static void ExtractV2TextParts(
        IEnumerable<IMessageComponent> components,
        List<string> parts)
    {
        foreach (var component in components)
        {
            switch (component)
            {
                case ContainerComponent c:
                    ExtractV2TextParts(c.Components, parts);
                    break;
                case SectionComponent s:
                    ExtractV2TextParts(s.Components, parts);
                    break;
                case TextDisplayComponent t when !string.IsNullOrWhiteSpace(t.Content):
                    parts.Add(t.Content);
                    break;
            }
        }
    }

    private static void AppendRenderedEmbed(
        ContainerBuilder container,
        string? authorName, string? authorUrl,
        string? title, string? embedUrl,
        string? description,
        IEnumerable<(string Name, string Value)> fields,
        string? footerText)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(authorName))
        {
            var display = authorName.StartsWith('@') ? authorName : $"@{authorName}";
            sb.AppendLine(!string.IsNullOrWhiteSpace(authorUrl)
                ? $"[{display}]({authorUrl})"
                : display);
        }

        if (!string.IsNullOrWhiteSpace(title))
            sb.AppendLine(!string.IsNullOrWhiteSpace(embedUrl)
                ? $"### [{title}]({embedUrl})"
                : $"### {title}");

        if (!string.IsNullOrWhiteSpace(description))
        {
            var desc = description.Length > 2500 ? $"{description[..2500]}…" : description;
            sb.AppendLine(desc);
        }

        foreach (var (name, value) in fields)
            sb.AppendLine($"**{name}**\n{value}");

        if (!string.IsNullOrWhiteSpace(footerText))
            sb.AppendLine($"-# {footerText}");

        var rendered = sb.ToString().TrimEnd();
        if (string.IsNullOrWhiteSpace(rendered)) return;

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        container.WithTextDisplay(rendered);
    }

    private static void AppendFileAttachments(ContainerBuilder container, IReadOnlyCollection<IAttachment> attachments)
    {
        var files = attachments
            .Where(a => !IsImageAttachment(a))
            .Select(a => $"- [{a.Filename}]({a.Url})")
            .Take(8)
            .ToList();

        if (files.Count > 0)
            container.WithTextDisplay(string.Join("\n", files));
    }

    private static void AppendMedia(
        ContainerBuilder container,
        IReadOnlyCollection<IAttachment> attachments,
        IReadOnlyCollection<IEmbed> embeds)
    {
        var media = new List<MediaGalleryItemProperties>();

        foreach (var attachment in attachments)
        {
            if (media.Count >= 10) break;
            if (IsImageAttachment(attachment))
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(attachment.Url)));
        }

        foreach (var embed in embeds)
        {
            if (media.Count >= 10) break;
            if (embed.Image?.Url is { } url && media.All(m => m.Media.Url != url))
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(url)));
        }

        if (media.Count > 0)
            container.WithMediaGallery(media);
    }

    private static string FormatHeader(string mention, DateTimeOffset timestamp)
        => $"{mention} · <t:{timestamp.ToUnixTimeSeconds()}:R>";

    private static bool IsImageUrl(string url)
    {
        var path = url.Split('?', '#')[0];
        return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageAttachment(IAttachment attachment)
        => attachment.Height is not null
           || attachment.Width is not null
           || (!string.IsNullOrWhiteSpace(attachment.ContentType)
               && attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
           || IsImageUrl(attachment.Url);
}
