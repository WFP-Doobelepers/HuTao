using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Services.Logging;
using HuTao.Services.Utilities;
using Serilog;
using Embed = HuTao.Data.Models.Discord.Message.Embeds.Embed;

namespace HuTao.Services.Quote;

public record JumpMessage(ulong GuildId, ulong ChannelId, ulong MessageId, bool Suppressed);

public record QuotedMessage(Context Context, ulong ChannelId, ulong MessageId, ulong UserId)
    : JumpMessage(Context.Guild.Id, ChannelId, MessageId, false);

public interface IQuoteService
{
    Task<List<MessageComponent>> BuildQuoteAsync(
        Context context,
        IUser requester,
        IEnumerable<JumpMessage> jumpUrls,
        bool expanded = false);

    Task<List<MessageComponent>> RebuildQuoteAsync(
        IGuild guild, IUser requester,
        ulong channelId, ulong messageId,
        bool expanded);
}

public class QuoteService(LoggingService logging, HuTaoContext db) : IQuoteService
{
    private const int MaxReplyDepth = 10;
    private const int WindowSize = 100;
    private const int MaxReplyContentLength = 200;
    private const string ReplyStart = "<:reply_right:1479788099457519758>";
    private const string ReplyEnd = "<:reply:1479788090942820476>";
    private const string ReplyChain = "<:reply_t:1479788095183261880>";
    private const string ReplyLine = "<:reply_line:1479788104394080326>";
    private const string ReplyDash = "<:reply_dash:1479788133800214701>";
    private const string ReplySpacer = "<:reply_spacer:1479788137512177716>";

    private const int MaxDisplayTextSize = 3800;

    private class ContainerAccumulator
    {
        private readonly List<ContainerBuilder> _containers = [];
        private ContainerBuilder _current = new();
        private int _currentTextSize;
        private readonly StringBuilder _textBuffer = new();

        public void AppendText(string text)
        {
            if (_textBuffer.Length > 0)
                _textBuffer.AppendLine();
            _textBuffer.Append(text);
        }

        public void FlushText()
        {
            if (_textBuffer.Length == 0) return;
            var remaining = _textBuffer.ToString();
            _textBuffer.Clear();

            Log.Debug("[Quote] FlushText: buffer={BufferLen}, currentSize={CurrentSize}, budget={Budget}",
                remaining.Length, _currentTextSize, MaxDisplayTextSize - _currentTextSize);

            while (remaining.Length > 0)
            {
                var budget = MaxDisplayTextSize - _currentTextSize;

                if (budget <= 0)
                {
                    Log.Debug("[Quote] FlushText: budget exhausted, flushing container #{Idx}", _containers.Count);
                    FlushContainer();
                    budget = MaxDisplayTextSize;
                }

                if (remaining.Length <= budget)
                {
                    _current.WithTextDisplay(remaining);
                    _currentTextSize += remaining.Length;
                    Log.Debug("[Quote] FlushText: fit in budget, newSize={CurrentSize}", _currentTextSize);
                    break;
                }

                var splitIdx = remaining.LastIndexOf('\n', Math.Min(budget, remaining.Length) - 1);
                if (splitIdx <= 0) splitIdx = budget;

                var chunk = remaining[..splitIdx];
                remaining = remaining[splitIdx..].TrimStart('\n');

                Log.Debug("[Quote] FlushText: splitting at {SplitIdx}, chunk={ChunkLen}, remaining={RemainingLen}",
                    splitIdx, chunk.Length, remaining.Length);

                _current.WithTextDisplay(chunk);
                _currentTextSize += chunk.Length;
                FlushContainer();
            }
        }

        public void AddMediaGallery(List<MediaGalleryItemProperties> media)
        {
            FlushText();
            _current.WithMediaGallery(media);
        }

        public void AddSeparator(bool isDivider = true, SeparatorSpacingSize spacing = SeparatorSpacingSize.Small)
        {
            FlushText();
            _current.WithSeparator(isDivider: isDivider, spacing: spacing);
        }

        public ContainerBuilder Current => _current;

        public void AddSection(SectionBuilder section)
        {
            FlushText();
            _current
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithSection(section);
        }

        private void FlushContainer()
        {
            _containers.Add(_current);
            _current = new ContainerBuilder();
            _currentTextSize = 0;
        }

        public List<ContainerBuilder> Build()
        {
            FlushText();
            _containers.Add(_current);
            Log.Debug("[Quote] Build: {Count} containers total", _containers.Count);
            return _containers;
        }
    }

    public async Task<List<MessageComponent>> BuildQuoteAsync(
        Context context, IUser requester,
        IEnumerable<JumpMessage> jumpUrls,
        bool expanded = false)
    {
        var jumpMessages = jumpUrls
            .Where(jump => !jump.Suppressed)
            .DistinctBy(j => j.MessageId)
            .ToList();

        var containers = new List<(ContainerBuilder Container, string JumpUrl, JumpMessage? Jump)>();

        foreach (var jump in jumpMessages)
        {
            var message = await jump.GetMessageAsync(context);
            if (message is not null)
            {
                var built = await BuildMessageContainer(message, expanded);
                foreach (var c in built)
                    containers.Add((c, message.GetJumpUrl(), jump));
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

            containers.Add((await BuildLogContainer(log), log.GetJumpUrl(), null));
        }

        if (containers.Count == 0)
            return [];

        AppendFooter(containers[^1], requester, expanded);

        var results = new List<MessageComponent>();
        foreach (var (container, _, _) in containers)
        {
            var builder = new ComponentBuilderV2();
            builder.WithContainer(container);
            results.Add(builder.Build());
        }

        return results;
    }

    public async Task<List<MessageComponent>> RebuildQuoteAsync(
        IGuild guild, IUser requester,
        ulong channelId, ulong messageId,
        bool expanded)
    {
        var channel = await guild.GetTextChannelAsync(channelId);
        if (channel is null) return [];

        var message = await channel.GetMessageAsync(messageId);
        if (message is null) return [];

        var containers = await BuildMessageContainer(message, expanded);
        if (containers.Count == 0) return [];

        var jumpUrl = $"https://discord.com/channels/{guild.Id}/{channelId}/{messageId}";
        var jump = new JumpMessage(guild.Id, channelId, messageId, false);

        var tagged = containers
            .Select(c => (Container: c, JumpUrl: jumpUrl, Jump: (JumpMessage?) jump))
            .ToList();

        AppendFooter(tagged[^1], requester, expanded);

        return tagged.Select(t =>
        {
            var builder = new ComponentBuilderV2();
            builder.WithContainer(t.Container);
            return builder.Build();
        }).ToList();
    }

    private static void AppendFooter(
        (ContainerBuilder Container, string JumpUrl, JumpMessage? Jump) entry,
        IUser requester, bool expanded)
    {
        entry.Container
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay($"-# Requested by {requester.Mention}");

        var row = new ActionRowBuilder()
            .WithButton(ButtonBuilder.CreateLinkButton("Jump", entry.JumpUrl));

        if (entry.Jump is not null)
        {
            var action = expanded ? "collapse" : "expand";
            var label = expanded ? "Collapse" : "Expand";
            row.WithButton(new ButtonBuilder(
                label,
                $"quote:{action}:{entry.Jump.ChannelId}:{entry.Jump.MessageId}:{requester.Id}",
                ButtonStyle.Secondary));
        }

        entry.Container.WithActionRow(row);
    }

    internal static async Task<List<ContainerBuilder>> BuildMessageContainer(IMessage message, bool expanded = false)
    {
        var acc = new ContainerAccumulator();

        var hasChain = await AppendReplyChain(acc, message, expanded);

        if (!hasChain)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-# {FormatHeader(message.Author.Mention, message.Timestamp)}");

            if (!string.IsNullOrWhiteSpace(message.Content))
                sb.Append(message.Content);

            acc.AppendText(sb.ToString().TrimEnd());
            acc.FlushText();

            foreach (var embed in message.Embeds)
                AppendRenderedEmbed(acc.Current, embed.Author?.Name, embed.Author?.Url,
                    embed.Title, embed.Url, embed.Description,
                    embed.Fields.Select(f => (f.Name, f.Value)),
                    embed.Footer?.Text);

            AppendComponentsV2Text(acc.Current, message.Components);
            AppendMedia(acc, message.Attachments, message.Embeds);
            AppendFileAttachments(acc.Current, message.Attachments);
        }

        return acc.Build();
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
            var imageUrl = embed.Image?.Url ?? embed.Url;
            if (imageUrl is not null && IsImageUrl(imageUrl) && media.All(m => m.Media.Url != imageUrl))
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(imageUrl)));
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

    private static async Task<bool> AppendReplyChain(
        ContainerAccumulator container, IMessage message, bool expanded = false)
    {
        var (chain, quoted, blockOf) = await ResolveBlocksAsync(message);

        Log.Debug("[Quote] {MessageId}: {Ancestors} ancestor block(s), quoted block of {Members}, {Blocks} block(s) in window (expanded={Expanded})",
            message.Id, chain.Count, quoted.Messages.Count, blockOf.Values.Distinct().Count(), expanded);

        if (chain.Count == 0) return false;

        if (expanded)
            AppendTreeMode(container, message.Id, chain, quoted, blockOf);
        else
            AppendFlatMode(container, message.Id, chain, quoted);

        return true;
    }

    private static async Task<(List<MessageBlock> Chain, MessageBlock Quoted, Dictionary<ulong, MessageBlock> BlockOf)>
        ResolveBlocksAsync(IMessage message)
    {
        IEnumerable<IMessage> window = [];
        try
        {
            // ponytail: one contiguous page ending at the quoted message. Ancestors further back are fetched
            // one at a time and lose their continuations; page backwards if that ever matters.
            window = await message.Channel
                .GetMessagesAsync(message.Id, Direction.Before, WindowSize)
                .FlattenAsync();
        }
        catch (HttpException ex)
        {
            Log.Warning(ex, "[Quote] Cannot read history before {MessageId}; quoting without continuations", message.Id);
        }

        var blockOf = MessageBlock.Partition(window.Append(message));
        var quoted = blockOf[message.Id];
        var chain = await MessageBlock.WalkChainAsync(quoted, blockOf, MaxReplyDepth, FetchParent);

        return (chain, quoted, blockOf);

        async Task<IMessage?> FetchParent(ulong id)
        {
            try
            {
                return await message.Channel.GetMessageAsync(id);
            }
            catch (HttpException ex)
            {
                Log.Warning(ex, "[Quote] Cannot fetch parent {ParentId} of {MessageId}; chain ends here", id, message.Id);
                return null;
            }
        }
    }

    private static void AppendFlatMode(
        ContainerAccumulator container, ulong quotedId,
        List<MessageBlock> chain, MessageBlock quoted)
    {
        var lastHadMedia = false;
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var isRoot = i == chain.Count - 1;
            if (!isRoot) container.AppendText($"-# {ReplyLine}");

            lastHadMedia = AppendBlock(container, chain[i],
                $"-# {(isRoot ? ReplyStart : ReplyChain)} ", $"-# {ReplyLine} ");
        }

        if (lastHadMedia)
            AppendQuotedBlock(container, quoted, quotedId, "");
        else
            AppendPlainBlock(container, quoted, quotedId);
    }

    private static void AppendTreeMode(
        ContainerAccumulator container, ulong quotedId,
        List<MessageBlock> chain, MessageBlock quoted,
        Dictionary<ulong, MessageBlock> blockOf)
    {
        var trunk = chain.Select(b => b.Head.Id).Append(quoted.Head.Id).ToHashSet();
        MessageBlock.LinkChildren(blockOf, trunk);

        var root = chain[^1];

        // Keep the trunk flat until the first branch, then nest from there on.
        // The quoted block always stays nested under its parent so "reply to X" reads differently from "sibling of X".
        var items = new List<MessageBlock>();
        var walk = root;
        while (walk.Children.Count == 1
               && trunk.Contains(walk.Children[0].Head.Id)
               && !walk.Children[0].Contains(quotedId))
        {
            var next = walk.Children[0];
            walk.Children.Clear();
            items.Add(next);
            walk = next;
        }

        items.AddRange(root.Children);

        AppendBlock(container, root, $"-# {ReplyStart} ", $"-# {ReplyLine} ");

        for (var i = 0; i < items.Count; i++)
        {
            container.AppendText($"-# {ReplyLine}");
            RenderTreeNode(container, items[i], [], i == items.Count - 1, quotedId);
        }
    }

    private static void RenderTreeNode(
        ContainerAccumulator container, MessageBlock node,
        List<bool> ancestorCols, bool isLast, ulong quotedId)
    {
        var prefix = string.Concat(ancestorCols.Select(c => c ? ReplyLine : ReplySpacer));

        if (node.Contains(quotedId))
        {
            AppendQuotedBlock(container, node, quotedId, prefix);
            return;
        }

        var hasMedia = node.Messages.Any(m => CollectMedia(m.Attachments, m.Embeds).Count > 0);
        var effectiveLast = isLast && !(hasMedia && node.Children.Count > 0);

        var connector = effectiveLast ? ReplyEnd : ReplyChain;
        var contentCol = effectiveLast ? ReplySpacer : ReplyLine;

        AppendBlock(container, node, $"-# {prefix}{connector} ", $"-# {prefix}{contentCol} ");

        var outdent = isLast && hasMedia && node.Children.Count > 0;
        var childPfx = outdent ? $"-# {prefix}" : $"-# {prefix}{(isLast ? ReplySpacer : ReplyLine)}";

        for (var j = 0; j < node.Children.Count; j++)
        {
            container.AppendText($"{childPfx}{ReplyLine}");
            RenderTreeNode(container, node.Children[j],
                outdent ? [.. ancestorCols] : [.. ancestorCols, !isLast],
                j == node.Children.Count - 1, quotedId);
        }
    }

    /// <summary>One header for the block, then every member's content and media in channel order.</summary>
    /// <returns>Whether the last thing rendered was a media gallery (the text column is broken).</returns>
    private static bool AppendBlock(
        ContainerAccumulator container, MessageBlock block,
        string headerPrefix, string contentPrefix)
    {
        container.AppendText($"{headerPrefix}{FormatHeader(block.Head.Author.Mention, block.Head.Timestamp)}");

        var lastHadMedia = false;
        foreach (var message in block.Messages)
        {
            var content = ExtractDisplayContent(message).TrimEnd();
            if (!string.IsNullOrWhiteSpace(content))
            {
                container.AppendText(PrefixLines(content, contentPrefix));
                lastHadMedia = false;
            }

            if (AppendMedia(container, message.Attachments, message.Embeds))
                lastHadMedia = true;
        }

        return lastHadMedia;
    }

    /// <summary>Block members Discord grouped before the quoted message, shown truncated as context.</summary>
    private static void AppendEarlierMembers(
        ContainerAccumulator container, MessageBlock block, ulong quotedId, string contentPrefix)
    {
        foreach (var message in block.Messages.TakeWhile(m => m.Id != quotedId))
        {
            var content = ExtractDisplayContent(message).TrimEnd();
            if (!string.IsNullOrWhiteSpace(content))
                container.AppendText(PrefixLines(content, contentPrefix));

            AppendMedia(container, message.Attachments, message.Embeds);
        }
    }

    private static void AppendPlainBlock(ContainerAccumulator container, MessageBlock block, ulong quotedId)
    {
        container.FlushText();
        container.AppendText(FormatHeader(block.Head.Author.Mention, block.Head.Timestamp));
        AppendEarlierMembers(container, block, quotedId, "-# ");

        var message = block.Messages.First(m => m.Id == quotedId);
        if (!string.IsNullOrWhiteSpace(message.Content))
            container.AppendText(message.Content.TrimEnd());

        container.FlushText();
        AppendMessageDetails(container, message);
    }

    private static void AppendQuotedBlock(
        ContainerAccumulator container, MessageBlock block, ulong quotedId, string emojiPrefix)
    {
        container.AppendText(
            $"-# {emojiPrefix}{ReplyEnd} {FormatHeader(block.Head.Author.Mention, block.Head.Timestamp)}");
        AppendEarlierMembers(container, block, quotedId, $"-# {emojiPrefix}{ReplySpacer} ");

        var message = block.Messages.First(m => m.Id == quotedId);
        if (!string.IsNullOrWhiteSpace(message.Content))
            container.AppendText(PrefixLines(message.Content.TrimEnd(), $"{emojiPrefix}{ReplySpacer} "));

        container.FlushText();
        AppendMessageDetails(container, message);
    }

    private static void AppendMessageDetails(ContainerAccumulator container, IMessage message)
    {
        foreach (var embed in message.Embeds)
            AppendRenderedEmbed(container.Current, embed.Author?.Name, embed.Author?.Url,
                embed.Title, embed.Url, embed.Description,
                embed.Fields.Select(f => (f.Name, f.Value)),
                embed.Footer?.Text);

        AppendComponentsV2Text(container.Current, message.Components);
        AppendMedia(container, message.Attachments, message.Embeds);
        AppendFileAttachments(container.Current, message.Attachments);
    }

    private async Task<bool> AppendLogReplyChain(ContainerBuilder container, MessageLog log)
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

        if (replies.Count == 0) return false;

        for (var i = replies.Count - 1; i >= 0; i--)
        {
            var reply = replies[i];
            var depth = replies.Count - 1 - i;
            var isDirectParent = i == 0;

            var sb = new StringBuilder();

            if (depth == 0)
            {
                sb.AppendLine($"-# {FormatHeader($"<@{reply.UserId}>", reply.Timestamp)}");
                var content = ExtractLogContent(reply);
                if (!string.IsNullOrWhiteSpace(content))
                    sb.Append(PrefixLines(content, $"-# {ReplyLine} "));
            }
            else
            {
                var headerIndent = ReplyLine + " " + string.Concat(Enumerable.Repeat(ReplySpacer + " ", depth - 1));
                var contentIndent = ReplyLine + " " + string.Concat(Enumerable.Repeat(ReplySpacer + " ", depth));

                sb.AppendLine($"-# {headerIndent}{ReplyEnd} {FormatHeader($"<@{reply.UserId}>", reply.Timestamp)}");
                var content = ExtractLogContent(reply);
                if (!string.IsNullOrWhiteSpace(content))
                    sb.Append(PrefixLines(content, $"-# {contentIndent}"));
            }

            container.WithTextDisplay(sb.ToString().TrimEnd());

            var replyMedia = reply.Attachments
                .Where(a => IsImageUrl(a.Url))
                .Take(10)
                .Select(a => new MediaGalleryItemProperties(new UnfurledMediaItemProperties(a.Url)))
                .ToList();

            if (replyMedia.Count > 0)
                container.WithMediaGallery(replyMedia);

            if (!isDirectParent)
            {
                var contentPrefix = ReplyLine + " " + string.Concat(Enumerable.Repeat(ReplySpacer + " ", depth));
                container.WithTextDisplay($"-# {contentPrefix.TrimEnd()}{ReplyLine}");
            }
        }

        return true;
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

    private static List<MediaGalleryItemProperties> CollectMedia(
        IReadOnlyCollection<IAttachment> attachments,
        IReadOnlyCollection<IEmbed> embeds)
    {
        var media = new List<MediaGalleryItemProperties>();

        foreach (var attachment in attachments)
        {
            if (media.Count >= 10) break;
            if (IsImageAttachment(attachment))
            {
                var url = attachment.ProxyUrl ?? attachment.Url;
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(url)));
            }
        }

        foreach (var embed in embeds)
        {
            if (media.Count >= 10) break;

            var imageUrl = embed.Image?.ProxyUrl ?? embed.Image?.Url;

            if (imageUrl is null && embed.Type == EmbedType.Image)
                imageUrl = embed.Url;

            if (imageUrl is not null && media.All(m => m.Media.Url != imageUrl))
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(imageUrl)));
        }

        return media;
    }

    private static bool AppendMedia(
        ContainerAccumulator acc,
        IReadOnlyCollection<IAttachment> attachments,
        IReadOnlyCollection<IEmbed> embeds,
        string? connectorBefore = null)
    {
        var media = CollectMedia(attachments, embeds);
        if (media.Count == 0) return false;
        if (connectorBefore is not null)
            acc.AppendText(connectorBefore);
        acc.AddMediaGallery(media);
        return true;
    }

    private static void AppendMedia(
        ContainerBuilder container,
        IReadOnlyCollection<IAttachment> attachments,
        IReadOnlyCollection<IEmbed> embeds)
    {
        var media = CollectMedia(attachments, embeds);
        if (media.Count > 0)
            container.WithMediaGallery(media);
    }

    private static string PrefixLines(string text, string prefix)
        => prefix + text.Replace("\n", $"\n{prefix}");

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
