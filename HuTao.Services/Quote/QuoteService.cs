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
            var text = _textBuffer.ToString();
            _textBuffer.Clear();
            if (_currentTextSize + text.Length > MaxDisplayTextSize)
                FlushContainer();
            _current.WithTextDisplay(text);
            _currentTextSize += text.Length;
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

    private static async Task<List<ContainerBuilder>> BuildMessageContainer(IMessage message, bool expanded = false)
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

    private record TreeNode(IMessage Message, List<TreeNode> Children);

    private static async Task<bool> AppendReplyChain(
        ContainerAccumulator container, IMessage message, bool expanded = false)
    {
        Log.Debug("[Quote] Walking reply chain from {MessageId} (expanded={Expanded})",
            message.Id, expanded);

        var chain = new List<IMessage>();
        var current = message;
        while (chain.Count < MaxReplyDepth && current.Reference?.MessageId.IsSpecified == true)
        {
            var parent = (current as IUserMessage)?.ReferencedMessage
                ?? await current.Channel.GetMessageAsync(current.Reference.MessageId.Value);
            if (parent is null) break;
            chain.Add(parent);
            current = parent;
        }

        if (chain.Count == 0) return false;

        var oldest = chain[^1];
        var window = (await message.Channel
                .GetMessagesAsync(oldest.Id, Direction.After, 100)
                .FlattenAsync())
            .Where(m => m.Timestamp <= message.Timestamp && m.Id != message.Id)
            .ToList();

        var chainIdSet = new HashSet<ulong>(chain.Select(m => m.Id));
        var standalones = window
            .Where(m => !chainIdSet.Contains(m.Id) && GetReferencedMessageId(m) is null)
            .OrderBy(m => m.Timestamp)
            .ToList();

        Log.Debug("[Quote] Chain: {Count} ancestors, Window: {WindowCount}, Standalones: {StandaloneCount}",
            chain.Count, window.Count, standalones.Count);

        if (expanded)
            AppendTreeMode(container, message, chain, window, chainIdSet, standalones);
        else
            AppendFlatMode(container, message, chain, standalones);

        return true;
    }

    private static void AppendFlatMode(
        ContainerAccumulator container, IMessage message,
        List<IMessage> chain, List<IMessage> standalones)
    {
        var ordered = chain.AsEnumerable().Reverse().ToList();
        var attached = AttachStandalonesToMessages(ordered, standalones);

        var root = ordered[0];
        var sb = new StringBuilder();
        sb.AppendLine($"-# {ReplyStart} {FormatHeader(root.Author.Mention, root.Timestamp)}");
        var rootContent = ExtractDisplayContent(root);
        if (!string.IsNullOrWhiteSpace(rootContent))
            sb.Append(PrefixLines(rootContent, $"-# {ReplyLine} "));
        container.AppendText(sb.ToString().TrimEnd());
        AppendMedia(container, root.Attachments, root.Embeds);
        AppendFlatStandalones(container, root, attached);

        for (var i = 1; i < ordered.Count; i++)
        {
            var node = ordered[i];
            container.AppendText($"-# {ReplyLine}");
            var nsb = new StringBuilder();
            nsb.AppendLine($"-# {ReplyChain} {FormatHeader(node.Author.Mention, node.Timestamp)}");
            var nodeContent = ExtractDisplayContent(node);
            if (!string.IsNullOrWhiteSpace(nodeContent))
                nsb.Append(PrefixLines(nodeContent, $"-# {ReplyLine} "));
            container.AppendText(nsb.ToString().TrimEnd());
            AppendMedia(container, node.Attachments, node.Embeds);
            AppendFlatStandalones(container, node, attached);
        }

        AppendPlainMessage(container, message);
    }

    private static void AppendFlatStandalones(
        ContainerAccumulator container, IMessage host, Dictionary<ulong, List<IMessage>> attached)
    {
        if (!attached.TryGetValue(host.Id, out var list)) return;
        foreach (var stan in list)
        {
            var ssb = new StringBuilder();
            if (stan.Author.Id != host.Author.Id)
            {
                ssb.AppendLine($"-# {ReplyLine}");
                ssb.AppendLine($"-# {ReplyLine} {FormatHeader(stan.Author.Mention, stan.Timestamp)}");
            }
            var content = ExtractDisplayContent(stan);
            if (!string.IsNullOrWhiteSpace(content))
                ssb.Append(PrefixLines(content, $"-# {ReplyLine} "));
            if (ssb.Length > 0)
            {
                container.AppendText(ssb.ToString().TrimEnd());
                AppendMedia(container, stan.Attachments, stan.Embeds);
            }
        }
    }

    private static void AppendTreeMode(
        ContainerAccumulator container, IMessage message,
        List<IMessage> chain, List<IMessage> window,
        HashSet<ulong> chainIdSet, List<IMessage> standalones)
    {
        var nodes = new Dictionary<ulong, TreeNode>();
        foreach (var m in chain)
            nodes[m.Id] = new TreeNode(m, []);

        var processed = new HashSet<ulong>();
        bool changed;
        do
        {
            changed = false;
            foreach (var m in window)
            {
                if (processed.Contains(m.Id) || nodes.ContainsKey(m.Id)) continue;
                var parentId = GetReferencedMessageId(m);
                if (parentId is null) { processed.Add(m.Id); continue; }
                if (nodes.ContainsKey(parentId.Value))
                {
                    nodes[m.Id] = new TreeNode(m, []);
                    changed = true;
                }
            }
        } while (changed);

        foreach (var node in nodes.Values)
        {
            var parentId = GetReferencedMessageId(node.Message);
            if (parentId is not null && nodes.TryGetValue(parentId.Value, out var parent))
                parent.Children.Add(node);
        }

        foreach (var node in nodes.Values)
            node.Children.Sort((a, b) =>
            {
                var ac = chainIdSet.Contains(a.Message.Id) ? 1 : 0;
                var bc = chainIdSet.Contains(b.Message.Id) ? 1 : 0;
                if (ac != bc) return ac - bc;
                return a.Message.Timestamp.CompareTo(b.Message.Timestamp);
            });

        var root = nodes[chain[^1].Id];

        var allTreeNodes = nodes.Values.OrderBy(n => n.Message.Timestamp).ToList();
        var attachedStandalones = new Dictionary<ulong, List<IMessage>>();
        foreach (var stan in standalones)
        {
            TreeNode? host = null;
            foreach (var n in allTreeNodes)
            {
                if (n.Message.Timestamp <= stan.Timestamp) host = n;
                else break;
            }
            if (host is null) continue;
            if (!attachedStandalones.ContainsKey(host.Message.Id))
                attachedStandalones[host.Message.Id] = [];
            attachedStandalones[host.Message.Id].Add(stan);
        }

        var flatItems = new List<TreeNode>();
        var walk = root;
        while (true)
        {
            var cc = walk.Children.FirstOrDefault(c => chainIdSet.Contains(c.Message.Id));
            if (cc is null || walk.Children.Count != 1) break;
            walk.Children.Remove(cc);
            flatItems.Add(cc);
            walk = cc;
        }

        // Insert quoted message into the tree as last child of its parent
        var quotedNode = new TreeNode(message, []);
        var parentNode = nodes[chain[0].Id];
        parentNode.Children.Add(quotedNode);

        var items = new List<TreeNode>();
        items.AddRange(flatItems);
        items.AddRange(root.Children);
        items.Sort((a, b) =>
        {
            var ac = chainIdSet.Contains(a.Message.Id) ? 1 : 0;
            var bc = chainIdSet.Contains(b.Message.Id) ? 1 : 0;
            if (ac != bc) return ac - bc;
            return a.Message.Timestamp.CompareTo(b.Message.Timestamp);
        });

        var sb = new StringBuilder();
        sb.AppendLine($"-# {ReplyStart} {FormatHeader(root.Message.Author.Mention, root.Message.Timestamp)}");
        var rootContent = ExtractDisplayContent(root.Message);
        if (!string.IsNullOrWhiteSpace(rootContent))
            sb.Append(PrefixLines(rootContent, $"-# {ReplyLine} "));
        container.AppendText(sb.ToString().TrimEnd());
        AppendMedia(container, root.Message.Attachments, root.Message.Embeds);
        RenderTreeStandalones(container, root.Message, $"-# {ReplyLine} ", attachedStandalones);

        for (var i = 0; i < items.Count; i++)
        {
            var isLast = i == items.Count - 1;
            container.AppendText($"-# {ReplyLine}");
            RenderTreeNode(container, items[i], [], isLast, attachedStandalones, message.Id);
        }
    }

    private static void RenderTreeNode(
        ContainerAccumulator container, TreeNode node,
        List<bool> ancestorCols, bool isLast,
        Dictionary<ulong, List<IMessage>> attached,
        ulong quotedMessageId = 0)
    {
        var isQuoted = node.Message.Id == quotedMessageId;
        var prefix = string.Concat(ancestorCols.Select(c => c ? ReplyLine : ReplySpacer));
        var connector = isLast ? ReplyEnd : ReplyChain;

        var contentCol = isLast ? ReplySpacer : ReplyLine;
        var contentPfx = $"-# {prefix}{contentCol}";

        if (isQuoted)
        {
            AppendQuotedMessage(container, node.Message, prefix);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"-# {prefix}{connector} {FormatHeader(node.Message.Author.Mention, node.Message.Timestamp)}");

        var content = ExtractDisplayContent(node.Message);
        if (!string.IsNullOrWhiteSpace(content))
            sb.Append(PrefixLines(content, $"{contentPfx} "));

        container.AppendText(sb.ToString().TrimEnd());
        AppendMedia(container, node.Message.Attachments, node.Message.Embeds);
        RenderTreeStandalones(container, node.Message, $"{contentPfx} ", attached);

        for (var j = 0; j < node.Children.Count; j++)
        {
            var childIsLast = j == node.Children.Count - 1;
            container.AppendText($"{contentPfx}{ReplyLine}");
            RenderTreeNode(container, node.Children[j], [.. ancestorCols, !isLast], childIsLast, attached, quotedMessageId);
        }
    }

    private static void RenderTreeStandalones(
        ContainerAccumulator container, IMessage host, string contentPfx,
        Dictionary<ulong, List<IMessage>> attached)
    {
        if (!attached.TryGetValue(host.Id, out var list)) return;
        foreach (var stan in list)
        {
            var ssb = new StringBuilder();
            if (stan.Author.Id != host.Author.Id)
            {
                ssb.AppendLine(contentPfx.TrimEnd());
                ssb.AppendLine($"{contentPfx}{FormatHeader(stan.Author.Mention, stan.Timestamp)}");
            }
            var sContent = ExtractDisplayContent(stan);
            if (!string.IsNullOrWhiteSpace(sContent))
                ssb.Append(PrefixLines(sContent, contentPfx));
            if (ssb.Length > 0)
            {
                container.AppendText(ssb.ToString().TrimEnd());
                AppendMedia(container, stan.Attachments, stan.Embeds);
            }
        }
    }

    private static void AppendPlainMessage(ContainerAccumulator container, IMessage message)
    {
        container.FlushText();

        var sb = new StringBuilder();
        sb.AppendLine(FormatHeader(message.Author.Mention, message.Timestamp));

        if (!string.IsNullOrWhiteSpace(message.Content))
            sb.Append(message.Content);

        container.AppendText(sb.ToString().TrimEnd());
        container.FlushText();

        foreach (var embed in message.Embeds)
            AppendRenderedEmbed(container.Current, embed.Author?.Name, embed.Author?.Url,
                embed.Title, embed.Url, embed.Description,
                embed.Fields.Select(f => (f.Name, f.Value)),
                embed.Footer?.Text);

        AppendComponentsV2Text(container.Current, message.Components);
        AppendMedia(container, message.Attachments, message.Embeds);
        AppendFileAttachments(container.Current, message.Attachments);
    }

    private static void AppendQuotedMessage(
        ContainerAccumulator container, IMessage message, string emojiPrefix)
    {
        var qsb = new StringBuilder();
        qsb.AppendLine($"-# {emojiPrefix}{ReplyEnd} {FormatHeader(message.Author.Mention, message.Timestamp)}");

        if (!string.IsNullOrWhiteSpace(message.Content))
            qsb.Append(PrefixLines(message.Content, $"{emojiPrefix}{ReplySpacer} "));

        container.AppendText(qsb.ToString().TrimEnd());
        container.FlushText();

        foreach (var embed in message.Embeds)
            AppendRenderedEmbed(container.Current, embed.Author?.Name, embed.Author?.Url,
                embed.Title, embed.Url, embed.Description,
                embed.Fields.Select(f => (f.Name, f.Value)),
                embed.Footer?.Text);

        AppendComponentsV2Text(container.Current, message.Components);
        AppendMedia(container, message.Attachments, message.Embeds);
        AppendFileAttachments(container.Current, message.Attachments);
    }

    private static Dictionary<ulong, List<IMessage>> AttachStandalonesToMessages(
        List<IMessage> hosts, List<IMessage> standalones)
    {
        var attached = new Dictionary<ulong, List<IMessage>>();
        foreach (var stan in standalones)
        {
            IMessage? host = null;
            foreach (var h in hosts)
            {
                if (h.Timestamp <= stan.Timestamp) host = h;
                else break;
            }
            if (host is null) continue;
            if (!attached.ContainsKey(host.Id)) attached[host.Id] = [];
            attached[host.Id].Add(stan);
        }
        return attached;
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

    private static void AppendMedia(
        ContainerAccumulator acc,
        IReadOnlyCollection<IAttachment> attachments,
        IReadOnlyCollection<IEmbed> embeds)
    {
        var media = CollectMedia(attachments, embeds);
        if (media.Count > 0)
            acc.AddMediaGallery(media);
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

    private static ulong? GetReferencedMessageId(IMessage message)
    {
        var messageId = message.Reference?.MessageId;
        if (messageId is null || !messageId.Value.IsSpecified)
            return null;

        return messageId.Value.Value;
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
