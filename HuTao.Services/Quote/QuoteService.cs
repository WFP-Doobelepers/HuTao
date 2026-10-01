using System;
using System.Collections.Generic;
using System.Globalization;
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

    /// <summary>The quote with nothing left out, over as many messages as it needs.</summary>
    Task<List<MessageComponent>> BuildFullQuoteAsync(IGuild guild, ulong channelId, ulong messageId, bool expanded);
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
    private const int MaxComponents = 40;
    private const int FooterComponents = 5;

    /// <param name="Truncated">Whether the quote left something out to fit in one message.</param>
    internal sealed record BuiltQuote(List<ContainerBuilder> Containers, bool Truncated);

    /// <summary>What a quote leaves out so that it fits in one message.</summary>
    /// <param name="DroppedLoose">How many loose turns to leave out, oldest first.</param>
    /// <param name="HiddenStart">The first hidden turn, counted in drawing order.</param>
    /// <param name="HiddenCount">How many turns in a row to hide.</param>
    /// <param name="QuotedTextLimit">The most characters of the quoted message's text to show.</param>
    private sealed record QuoteCut(
        int DroppedLoose = 0, int HiddenStart = 0, int HiddenCount = 0, int? QuotedTextLimit = null)
    {
        public static readonly QuoteCut None = new();
    }

    /// <summary>The linked turns of a quote, collected once and drawn as many times as fitting it takes.</summary>
    /// <param name="Loose">The loose turns that will show, oldest first.</param>
    /// <param name="TurnCount">How many turns the layout draws without the loose turns.</param>
    private sealed record Conversation(
        MessageBlock Root, IReadOnlySet<ulong> Trunk, List<MessageBlock> Loose, int TurnCount);

    /// <param name="target">
    ///     When set, each container aims at this much text instead of filling up to the limit, so the parts of a long
    ///     quote come out about the same size.
    /// </param>
    private class ContainerAccumulator(int? target = null)
    {
        private readonly List<ContainerBuilder> _containers = [];
        private ContainerBuilder _current = new();
        private int _currentTextSize;
        private readonly StringBuilder _textBuffer = new();
        private readonly List<int> _breaks = [];

        public void AppendText(string text)
        {
            if (_textBuffer.Length > 0)
                _textBuffer.AppendLine();
            _textBuffer.Append(text);
        }

        /// <summary>Marks the start of the next text as a good place to end a message, because a turn starts there.</summary>
        public void MarkBreak()
        {
            if (_textBuffer.Length > 0)
                _breaks.Add(_textBuffer.Length);
        }

        public void FlushText()
        {
            if (_textBuffer.Length == 0) return;
            var remaining = _textBuffer.ToString();
            var breaks = _breaks.ToList();
            var consumed = 0;
            _textBuffer.Clear();
            _breaks.Clear();

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

                // With a target, a container ends near it and never past the limit. A little over the target is
                // allowed, so that the last part does not spill into a small extra one.
                var goal = Math.Min(budget, (target ?? MaxDisplayTextSize) - _currentTextSize);
                var room = target is { } share ? Math.Min(budget, goal + share / 4) : budget;

                if (remaining.Length <= room)
                {
                    _current.WithTextDisplay(remaining);
                    _currentTextSize += remaining.Length;
                    Log.Debug("[Quote] FlushText: fit in budget, newSize={CurrentSize}", _currentTextSize);
                    break;
                }

                if (goal <= 0)
                {
                    FlushContainer();
                    continue;
                }

                // Rails cannot cross from one message to the next, so end the message where a turn starts: the header
                // then stays with its text. Without a target, take the last turn start in the second half; with one,
                // the turn start nearest to it. Fall back to any line break.
                var starts = breaks.Select(b => b - consumed).Where(b => b > 0 && b <= budget).ToList();
                var splitIdx = target is null
                    ? starts.Where(b => b > budget / 2).DefaultIfEmpty(-1).Max()
                    : starts.OrderBy(b => Math.Abs(b - goal)).DefaultIfEmpty(-1).First();
                if (splitIdx <= 0) splitIdx = remaining.LastIndexOf('\n', Math.Min(goal, remaining.Length) - 1);
                if (splitIdx <= 0) splitIdx = budget;

                var chunk = remaining[..splitIdx].TrimEnd('\r', '\n');
                var rest = remaining[splitIdx..];
                remaining = rest.TrimStart('\r', '\n');
                consumed += splitIdx + rest.Length - remaining.Length;

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
                var built = await BuildQuote(message, expanded);
                foreach (var c in built.Containers)
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

        var built = await BuildQuote(message, expanded);
        if (built.Containers.Count == 0) return [];

        var jumpUrl = $"https://discord.com/channels/{guild.Id}/{channelId}/{messageId}";
        var jump = new JumpMessage(guild.Id, channelId, messageId, false);

        var tagged = built.Containers
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

    public async Task<List<MessageComponent>> BuildFullQuoteAsync(
        IGuild guild, ulong channelId, ulong messageId, bool expanded)
    {
        var channel = await guild.GetTextChannelAsync(channelId);
        if (channel is null) return [];

        var message = await channel.GetMessageAsync(messageId);
        if (message is null) return [];

        var containers = await BuildMessageContainer(message, expanded, oneMessage: false);
        containers[^1].WithActionRow(new ActionRowBuilder()
            .WithButton(ButtonBuilder.CreateLinkButton("Jump", message.GetJumpUrl())));

        return containers.Select(c => new ComponentBuilderV2().WithContainer(c).Build()).ToList();
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

    internal static async Task<List<ContainerBuilder>> BuildMessageContainer(
        IMessage message, bool expanded = false, bool oneMessage = true, int budget = MaxDisplayTextSize)
        => (await BuildQuote(message, expanded, oneMessage, budget)).Containers;

    /// <summary>
    ///     Builds the quote of <paramref name="message" />. With <paramref name="oneMessage" />, the quote always fits in
    ///     one Discord message: it leaves out loose turns first (oldest first), then hides turns from the middle, then
    ///     the root, and last shortens the quoted message, until it fits. A "truncated" line stands where something was
    ///     left out. Without <paramref name="oneMessage" />, nothing is left out, and a long quote takes several
    ///     containers.
    /// </summary>
    internal static async Task<BuiltQuote> BuildQuote(
        IMessage message, bool expanded = false, bool oneMessage = true, int budget = MaxDisplayTextSize)
    {
        var conversation = await CollectAsync(message, expanded);

        var quote = Render(message, conversation, expanded, QuoteCut.None);
        if (!oneMessage)
        {
            // Nothing is left out, so a long quote takes several messages. They share the text about evenly,
            // instead of one full message followed by a scrap.
            var text = Measure(quote).Text;
            var parts = (int) Math.Ceiling(text / (double) MaxDisplayTextSize);
            if (parts > 1)
                quote = Render(message, conversation, expanded, QuoteCut.None, text / parts + 1);

            return new BuiltQuote(quote, false);
        }

        if (Fits(quote, budget)) return new BuiltQuote(quote, false);

        var loose = conversation?.Loose.Count ?? 0;
        var turns = conversation?.TurnCount ?? 1;
        foreach (var cut in Cuts(loose, turns))
        {
            quote = Render(message, conversation, expanded, cut);
            if (Fits(quote, budget)) return new BuiltQuote(quote, true);
        }

        // The quoted turn alone is still too long, so its text gets the room that is left.
        var smallest = new QuoteCut(loose, 0, turns - 1);
        var room = budget - Measure(Render(message, conversation, expanded, smallest with { QuotedTextLimit = 0 })).Text;
        var limit = room - "…".Length - Environment.NewLine.Length;
        if (limit > 0)
            quote = Render(message, conversation, expanded, smallest with { QuotedTextLimit = limit });

        return new BuiltQuote(quote, true);
    }

    /// <summary>The ways to shrink a quote, from the least left out to the most.</summary>
    private static IEnumerable<QuoteCut> Cuts(int loose, int turns)
    {
        for (var dropped = 1; dropped <= loose; dropped++)
            yield return new QuoteCut(dropped);

        // Hide turns from the middle outward. The root (first) and the quoted turn (last) stay.
        var middle = turns - 2;
        for (var hidden = 1; hidden <= middle; hidden++)
            yield return new QuoteCut(loose, 1 + (middle - hidden) / 2, hidden);

        if (turns >= 2)
            yield return new QuoteCut(loose, 0, turns - 1);
    }

    /// <param name="target">The text each container aims at, when a long quote is split into several.</param>
    private static List<ContainerBuilder> Render(
        IMessage message, Conversation? conversation, bool expanded, QuoteCut cut, int? target = null)
    {
        // Discord rejects a message with two buttons of the same custom id, so each "truncated" line numbers its
        // button: quote:all:<channel>:<message>:<mode>.<n>. The handler reads the mode before the dot.
        var buttons = 0;
        string showAll() => $"quote:all:{message.Channel.Id}:{message.Id}:{(expanded ? 1 : 0)}.{++buttons}";
        var acc = new ContainerAccumulator(target);
        if (conversation is null)
            AppendPlain(acc, message, cut, showAll);
        else
            AppendRail(acc, conversation, message, cut, showAll);

        return acc.Build();
    }

    /// <summary>A quote fits when it is one container within the text budget and the component limit.</summary>
    private static bool Fits(List<ContainerBuilder> quote, int budget)
    {
        if (quote.Count != 1) return false;

        var (text, count) = Measure(quote[0].Components);
        return text <= budget && 1 + count + FooterComponents <= MaxComponents;
    }

    private static (int Text, int Count) Measure(IEnumerable<IMessageComponentBuilder> components)
    {
        var text = 0;
        var count = 0;
        foreach (var component in components)
        {
            count++;
            switch (component)
            {
                case ContainerBuilder container:
                    var (innerText, innerCount) = Measure(container.Components);
                    text += innerText;
                    count += innerCount;
                    break;
                case TextDisplayBuilder display:
                    text += display.Content?.Length ?? 0;
                    break;
                case SectionBuilder section:
                    var (sectionText, sectionCount) = Measure(section.Components);
                    text += sectionText;
                    count += sectionCount + (section.Accessory is null ? 0 : 1);
                    break;
                case ActionRowBuilder row:
                    count += row.Components.Count;
                    break;
            }
        }

        return (text, count);
    }

    private static string Truncated(int messages)
        => $"{messages} {(messages == 1 ? "message" : "messages")} truncated";

    private static string Truncated(string what, int count)
        => $"{count.ToString("N0", CultureInfo.InvariantCulture)} {what} truncated";

    /// <summary>
    ///     Stands exactly where a quote left something out: a separator, one "truncated" line with a "Show all" button
    ///     beside it, and a separator again. The rails stop above it and carry on below it.
    /// </summary>
    /// <param name="showAll">Gives the button's custom id; the button sends the whole quote to whoever presses it.</param>
    private static void AppendCut(ContainerAccumulator acc, string line, Func<string> showAll)
    {
        acc.AddSeparator();
        acc.Current.WithSection(
            [new TextDisplayBuilder($"-# {line}")],
            new ButtonBuilder("Show all", showAll(), ButtonStyle.Secondary));
        acc.AddSeparator();
    }

    private static void AppendPlain(ContainerAccumulator acc, IMessage message, QuoteCut cut, Func<string> showAll)
    {
        var (content, removed) = Shorten(message.Content, cut.QuotedTextLimit);

        var sb = new StringBuilder();
        sb.AppendLine($"-# {FormatHeader(AuthorLabel(message.Author), message.Timestamp)}");
        if (!string.IsNullOrWhiteSpace(content))
            sb.Append(content);

        acc.AppendText(sb.ToString().TrimEnd());
        acc.FlushText();

        if (removed > 0)
            AppendCut(acc, Truncated("characters", removed), showAll);

        foreach (var embed in message.Embeds)
            AppendRenderedEmbed(acc.Current, embed.Author?.Name, embed.Author?.Url,
                embed.Title, embed.Url, embed.Description,
                embed.Fields.Select(f => (f.Name, f.Value)),
                embed.Footer?.Text);

        AppendComponentsV2Text(acc.Current, message.Components);
        AppendMedia(acc, message.Attachments, message.Embeds);
        AppendFileAttachments(acc.Current, message.Attachments);
    }

    /// <summary>
    ///     The quoted message's text, cut to <paramref name="limit" /> characters with an ellipsis if longer, and how
    ///     many characters were cut.
    /// </summary>
    private static (string Text, int Removed) Shorten(string? text, int? limit)
    {
        var trimmed = text?.TrimEnd() ?? "";
        if (limit is not { } max || trimmed.Length <= max) return (trimmed, 0);
        return (max > 0 ? $"{trimmed[..max]}…" : "", trimmed.Length - Math.Max(max, 0));
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

    /// <returns>The linked conversation, or null when the quoted message has no reply chain.</returns>
    private static async Task<Conversation?> CollectAsync(IMessage message, bool expanded)
    {
        var (chain, quoted, blockOf) = await ResolveBlocksAsync(message);

        Log.Debug("[Quote] {MessageId}: {Ancestors} ancestor block(s), quoted block of {Members}, {Blocks} block(s) in window (expanded={Expanded})",
            message.Id, chain.Count, quoted.Messages.Count, blockOf.Values.Distinct().Count(), expanded);

        if (chain.Count == 0) return null;

        // The modes differ only in what they collect. Collapsed links the reply chain and the direct replies to it,
        // everything on one straight rail. Expanded links every block in the window, so the threads under those
        // replies show too, and adds the loose turns in between. One layout draws both.
        var root = chain[^1];
        var trunk = chain.Select(b => b.Head.Id).Append(quoted.Head.Id).ToHashSet();
        List<MessageBlock> loose = [];
        if (expanded)
        {
            MessageBlock.LinkChildren(blockOf, trunk);
            var linked = QuoteLayout.Reachable(root);
            loose = blockOf.Values.Distinct()
                .Where(b => !linked.Contains(b) && b.Head.Id > root.Head.Id && IsLooseTurn(b))
                .OrderBy(b => b.Head.Id)
                .ToList();
        }
        else
            MessageBlock.LinkMainRail(blockOf, trunk);

        var turns = QuoteLayout.Layout(root, trunk).Count(row => row is QuoteLayout.TurnRow);
        return new Conversation(root, trunk, loose, turns);
    }

    /// <summary>
    ///     Whether an unconnected turn shows in expanded mode. Posts by bots stay out, so the bot's own earlier quotes
    ///     do not appear inside a new one. Webhook posts stay in, because people talk through webhooks.
    /// </summary>
    private static bool IsLooseTurn(MessageBlock block)
        => MessageBlock.IsUserMessage(block.Head)
            && !(block.Head.Author.IsBot && !block.Head.Author.IsWebhook);

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

    private static void AppendRail(
        ContainerAccumulator container, Conversation conversation, IMessage message, QuoteCut cut,
        Func<string> showAll)
    {
        var dropped = conversation.Loose.Take(cut.DroppedLoose).ToHashSet();


        // A segment is one turn with the gap rows below it. The loose turns stay in the layout even when they are
        // left out, so that the line saying so stands where they were.
        var segments = new List<(List<QuoteLayout.Row> Rows, bool Removed)>();
        var turn = 0;
        foreach (var row in QuoteLayout.Layout(conversation.Root, conversation.Trunk, conversation.Loose))
        {
            switch (row)
            {
                case QuoteLayout.TurnRow:
                    segments.Add(([row], turn >= cut.HiddenStart && turn < cut.HiddenStart + cut.HiddenCount));
                    turn++;
                    break;
                case QuoteLayout.LooseRow loose:
                    segments.Add(([row], dropped.Contains(loose.Block)));
                    break;
                default:
                    if (segments.Count == 0)
                        segments.Add(([row], false));
                    else
                        segments[^1].Rows.Add(row);
                    break;
            }
        }

        for (var i = 0; i < segments.Count; i++)
        {
            if (!segments[i].Removed)
            {
                foreach (var row in segments[i].Rows)
                    AppendRow(row);
                continue;
            }

            // Segments removed next to each other become one "truncated" line, where they were.
            var end = i;
            while (end + 1 < segments.Count && segments[end + 1].Removed) end++;
            var run = segments.GetRange(i, end - i + 1);
            var messages = run.Sum(s => BlockOf(s.Rows[0])?.Messages.Count ?? 0);

            AppendCut(container, Truncated(messages), showAll);
            i = end;
        }

        void AppendRow(QuoteLayout.Row row)
        {
            switch (row)
            {
                case QuoteLayout.GapRow gap:
                    container.AppendText($"-# {Emoji(gap.Cells)}");
                    break;
                case QuoteLayout.LooseRow loose:
                    container.MarkBreak();
                    AppendBlock(container, loose.Block, Prefix(loose.Cells), Prefix(loose.Cells));
                    break;
                case QuoteLayout.TurnRow quoted when quoted.Block.Contains(message.Id):
                    container.MarkBreak();
                    AppendQuotedTurn(container, quoted, message.Id, cut.QuotedTextLimit, showAll);
                    break;
                case QuoteLayout.TurnRow other:
                    container.MarkBreak();
                    AppendBlock(container, other.Block, Prefix(other.Cells), Prefix(other.ContentCells));
                    break;
            }
        }

        static MessageBlock? BlockOf(QuoteLayout.Row row) => row switch
        {
            QuoteLayout.TurnRow t  => t.Block,
            QuoteLayout.LooseRow t => t.Block,
            _                      => null
        };
    }

    /// <summary>
    ///     The whole turn: the author's header, their messages, and a fresh header wherever a long pause makes
    ///     Discord draw one. The extra headers sit in the content column, unconnected to the reply tree.
    /// </summary>
    /// <remarks>An image breaks the text, so the rails stop above it and carry on below it.</remarks>
    private static void AppendBlock(
        ContainerAccumulator container, MessageBlock block,
        string headerPrefix, string contentPrefix)
    {
        var first = true;

        foreach (var (message, startsHeader) in block.Flattened())
        {
            if (startsHeader)
            {
                AppendGroupHeader(container, message, first ? headerPrefix : contentPrefix, first, contentPrefix);
                first = false;
            }

            var content = ExtractDisplayContent(message).TrimEnd();
            if (!string.IsNullOrWhiteSpace(content))
                container.AppendText(PrefixLines(content, contentPrefix));

            AppendMedia(container, message.Attachments, message.Embeds);
        }
    }

    private static void AppendGroupHeader(
        ContainerAccumulator container, IMessage message, string prefix, bool first, string contentPrefix)
    {
        if (!first) container.AppendText(contentPrefix.TrimEnd());
        container.AppendText($"{prefix}{FormatHeader(AuthorLabel(message.Author), message.Timestamp)}");
    }

    /// <summary>
    ///     Renders the quoted message's own turn: earlier messages of that turn as truncated context, extra
    ///     headers where Discord would draw them, then the quoted message itself at full length.
    /// </summary>
    private static void AppendOwnBlock(
        ContainerAccumulator container, MessageBlock block, ulong quotedId,
        string firstHeaderPrefix, string contentPrefix)
    {
        var first = true;

        foreach (var (message, startsHeader) in block.Flattened())
        {
            if (startsHeader)
            {
                AppendGroupHeader(container, message, first ? firstHeaderPrefix : contentPrefix, first, contentPrefix);
                first = false;
            }

            if (message.Id == quotedId) break;

            var content = ExtractDisplayContent(message).TrimEnd();
            if (!string.IsNullOrWhiteSpace(content))
                container.AppendText(PrefixLines(content, contentPrefix));

            AppendMedia(container, message.Attachments, message.Embeds);
        }
    }

    /// <summary>
    ///     The quoted message's turn. At the end of the main rail it has no gutter, so the line above runs straight
    ///     into its header, and the quoted message itself is full size.
    /// </summary>
    private static void AppendQuotedTurn(
        ContainerAccumulator container, QuoteLayout.TurnRow turn, ulong quotedId, int? textLimit,
        Func<string> showAll)
    {
        var header = turn.Cells.Count == 0 ? "" : Prefix(turn.Cells);
        AppendOwnBlock(container, turn.Block, quotedId, header, Prefix(turn.ContentCells));

        var message = turn.Block.Messages.First(m => m.Id == quotedId);
        var (content, removed) = Shorten(message.Content, textLimit);
        if (!string.IsNullOrWhiteSpace(content))
        {
            var body = turn.ContentCells.Count == 0 ? "" : $"{Emoji(turn.ContentCells)} ";
            container.AppendText(PrefixLines(content, body));
        }

        if (removed > 0)
            AppendCut(container, Truncated("characters", removed), showAll);

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

    private static string PrefixLines(string text, string prefix)
        => prefix + text.Replace("\n", $"\n{prefix}");

    /// <summary>The subtext prefix for a line after these gutter cells: <c>-# </c>, the emoji, then a space.</summary>
    private static string Prefix(IReadOnlyList<RailCell> cells)
        => cells.Count == 0 ? "-# " : $"-# {Emoji(cells)} ";

    private static string Emoji(IEnumerable<RailCell> cells) => string.Concat(cells.Select(cell => cell switch
    {
        RailCell.Line => ReplyLine,
        RailCell.Tee  => ReplyChain,
        RailCell.Top  => ReplyStart,
        RailCell.End  => ReplyEnd,
        _             => ReplySpacer
    }));

    /// <summary>A webhook has no member to mention, so its post shows the display name it was sent with.</summary>
    private static string AuthorLabel(IUser author)
        => author.IsWebhook ? $"**{author.Username}**" : author.Mention;

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
