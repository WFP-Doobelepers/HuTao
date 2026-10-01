using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace HuTao.Services.Quote;

/// <summary>
///     One author's uninterrupted turn: a head message plus the messages that same author sent straight after it,
///     with nobody else in between and no new reply among them.
/// </summary>
/// <remarks>
///     A long pause does not end the turn, it only makes the Discord client draw a second header — see
///     <see cref="Flattened" />, which mirrors the client's <c>shouldStartNewGroup</c>: the gap is measured from
///     the group's first message, not the previous one, and is strict. Skipped on purpose: the viewer-local
///     "same calendar day" check (the bot has no viewer timezone), ephemeral/silent/scheduled flags.
/// </remarks>
public sealed class MessageBlock(IMessage head)
{
    public static readonly TimeSpan MaxGroupGap = TimeSpan.FromMinutes(7);

    public IMessage Head { get; } = head;

    public List<IMessage> Messages { get; } = [head];

    public List<MessageBlock> Children { get; } = [];

    /// <summary>The message this block's head replies to, if any.</summary>
    public ulong? ParentId => ReferencedMessageId(Head);

    public bool Contains(ulong messageId) => Messages.Any(m => m.Id == messageId);

    /// <summary>
    ///     The message an inline reply points at. Forwards, pin notices and thread starters carry a reference too,
    ///     but they are not replies.
    /// </summary>
    public static ulong? ReferencedMessageId(IMessage message)
        => message.Type == MessageType.Reply && message.Reference?.MessageId is { IsSpecified: true } id
            ? id.Value
            : null;

    /// <summary>Whether <paramref name="current" /> is the same author still talking, uninterrupted.</summary>
    public static bool IsContinuation(IMessage head, IMessage current)
        => current.Type == MessageType.Default
            && current.Flags?.HasFlag(MessageFlags.HasThread) is not true
            && IsUserMessage(head)
            && current.Author.Id == head.Author.Id
            && (!current.Author.IsWebhook || current.Author.Username == head.Author.Username);

    /// <summary>
    ///     Every message in the turn, flagged with whether Discord would draw a fresh author header above it
    ///     because it landed <see cref="MaxGroupGap" /> or more after the current header's message.
    /// </summary>
    public IEnumerable<(IMessage Message, bool StartsHeader)> Flattened()
    {
        IMessage? groupHead = null;
        foreach (var message in Messages)
        {
            var starts = groupHead is null || message.Timestamp - groupHead.Timestamp >= MaxGroupGap;
            if (starts) groupHead = message;

            yield return (message, starts);
        }
    }

    internal static bool IsUserMessage(IMessage message)
        => message.Type is MessageType.Default or MessageType.Reply
            or MessageType.ApplicationCommand or MessageType.ContextMenuCommand;

    /// <summary>Groups messages the way the Discord client draws them.</summary>
    /// <param name="contiguous">Messages with no gaps between them in channel order; any input order is fine.</param>
    /// <returns>Every message id mapped to the block it belongs to.</returns>
    public static Dictionary<ulong, MessageBlock> Partition(IEnumerable<IMessage> contiguous)
    {
        var blockOf = new Dictionary<ulong, MessageBlock>();
        MessageBlock? current = null;

        foreach (var message in contiguous.DistinctBy(m => m.Id).OrderBy(m => m.Id))
        {
            if (current is not null && IsContinuation(current.Head, message))
                current.Messages.Add(message);
            else
                current = new MessageBlock(message);

            blockOf[message.Id] = current;
        }

        return blockOf;
    }

    /// <summary>Follows reply references upward from <paramref name="from" />. Nearest ancestor first.</summary>
    /// <remarks>
    ///     A parent outside <paramref name="blockOf" /> is fetched and registered as a single-message block, so its
    ///     own continuations are unknown.
    /// </remarks>
    public static async Task<List<MessageBlock>> WalkChainAsync(
        MessageBlock from, Dictionary<ulong, MessageBlock> blockOf, int maxDepth,
        Func<ulong, Task<IMessage?>> fetch)
    {
        var chain = new List<MessageBlock>();
        var current = from;

        while (chain.Count < maxDepth && current.ParentId is { } parentId)
        {
            if (!blockOf.TryGetValue(parentId, out var parent))
            {
                var message = (current.Head as IUserMessage)?.ReferencedMessage ?? await fetch(parentId);
                if (message is null) break;

                blockOf[parentId] = parent = new MessageBlock(message);
            }

            if (parent == from || chain.Contains(parent)) break;

            chain.Add(parent);
            current = parent;
        }

        return chain;
    }

    /// <summary>
    ///     Attaches every block to the block containing the message its head replies to. Children are in channel
    ///     order, except blocks on the <paramref name="trunk" /> (the quoted message's own ancestry) sort last.
    /// </summary>
    public static void LinkChildren(Dictionary<ulong, MessageBlock> blockOf, HashSet<ulong> trunk)
    {
        var blocks = blockOf.Values.Distinct().ToList();

        foreach (var block in blocks)
        {
            if (block.ParentId is { } parentId
                && blockOf.TryGetValue(parentId, out var parent)
                && parent != block)
                parent.Children.Add(block);
        }

        foreach (var block in blocks)
        {
            block.Children.Sort((a, b) =>
            {
                var byTrunk = trunk.Contains(a.Head.Id).CompareTo(trunk.Contains(b.Head.Id));
                return byTrunk != 0 ? byTrunk : a.Head.Id.CompareTo(b.Head.Id);
            });
        }
    }

    /// <summary>
    ///     Links the <paramref name="trunk" /> and every direct reply to a trunk block, but not the replies to those
    ///     replies. That is everything on one straight rail, which is what collapsed mode shows.
    /// </summary>
    public static void LinkMainRail(Dictionary<ulong, MessageBlock> blockOf, HashSet<ulong> trunk)
    {
        LinkChildren(blockOf, trunk);

        foreach (var block in blockOf.Values.Distinct().Where(b => !trunk.Contains(b.Head.Id)))
            block.Children.Clear();
    }
}
