using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HuTao.Services.Quote;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Quote;

public class MessageBlockTests
{
    private const ulong PersonA = 100;
    private const ulong Hime = 200;
    private const ulong PersonB = 300;

    private static readonly DateTimeOffset T0 = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    private static IMessage Msg(ulong id, ulong author, int secondsAfterT0, ulong? replyTo = null,
        MessageType? type = null)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(author);
        user.SetupGet(u => u.Mention).Returns($"<@{author}>");

        var message = new Mock<IUserMessage>();
        message.SetupGet(m => m.Id).Returns(id);
        message.SetupGet(m => m.Author).Returns(user.Object);
        message.SetupGet(m => m.Timestamp).Returns(T0.AddSeconds(secondsAfterT0));
        message.SetupGet(m => m.Type).Returns(type ?? (replyTo is null ? MessageType.Default : MessageType.Reply));
        message.SetupGet(m => m.Reference).Returns(replyTo is null ? null! : new MessageReference(replyTo));
        message.SetupGet(m => m.Content).Returns($"msg-{id}");
        return message.Object;
    }

    private static Task<IMessage?> NoFetch(ulong _) => Task.FromResult<IMessage?>(null);

    private static IEnumerable<MessageBlock> AllChildren(Dictionary<ulong, MessageBlock> blockOf)
        => blockOf.Values.Distinct().SelectMany(b => b.Children);

    // The scenario from the bug report, verbatim:
    //   person A: <something>
    //   hime-san (replying):      I see, if that's the case we should do that
    //   hime-san (not replying):  And then we should also do this          -> same block as the reply
    //   person B (not replying):  Good morning!                            -> not part of anything
    //   person B (replying to hime-san): Ohh really? This is interesting!  -> nested under hime-san
    private static List<IMessage> Scenario(ulong personBRepliesTo) =>
    [
        Msg(1, PersonA, 0),
        Msg(2, Hime, 60, replyTo: 1),
        Msg(3, Hime, 90),
        Msg(4, PersonB, 120),
        Msg(5, PersonB, 150, replyTo: personBRepliesTo),
    ];

    [Fact]
    public async Task Continuation_joins_the_reply_block_and_unrelated_message_is_excluded()
    {
        var blockOf = MessageBlock.Partition(Scenario(personBRepliesTo: 2));
        var quoted = blockOf[5];
        var chain = await MessageBlock.WalkChainAsync(quoted, blockOf, 10, NoFetch);
        MessageBlock.LinkChildren(blockOf, chain.Select(b => b.Head.Id).Append(quoted.Head.Id).ToHashSet());

        Assert.Same(blockOf[2], blockOf[3]);
        Assert.Equal([2UL, 3UL], blockOf[2].Messages.Select(m => m.Id));

        Assert.Equal([2UL, 1UL], chain.Select(b => b.Head.Id));
        Assert.Equal([blockOf[2]], blockOf[1].Children);
        Assert.Equal([quoted], blockOf[2].Children);

        Assert.DoesNotContain(blockOf[4], AllChildren(blockOf));
        Assert.DoesNotContain(blockOf[4], chain);
    }

    [Fact]
    public async Task Reply_to_root_is_a_sibling_at_the_root_depth_and_sorts_after_the_branch()
    {
        var blockOf = MessageBlock.Partition(Scenario(personBRepliesTo: 1));
        var quoted = blockOf[5];
        var chain = await MessageBlock.WalkChainAsync(quoted, blockOf, 10, NoFetch);
        MessageBlock.LinkChildren(blockOf, chain.Select(b => b.Head.Id).Append(quoted.Head.Id).ToHashSet());

        Assert.Equal([1UL], chain.Select(b => b.Head.Id));
        Assert.Equal([blockOf[2], quoted], blockOf[1].Children);
        Assert.Empty(blockOf[2].Children);
    }

    [Fact]
    public async Task Reply_to_a_continuation_resolves_to_the_block_and_the_chain_continues_through_its_head()
    {
        var blockOf = MessageBlock.Partition(Scenario(personBRepliesTo: 3));
        var quoted = blockOf[5];
        var chain = await MessageBlock.WalkChainAsync(quoted, blockOf, 10, NoFetch);

        Assert.Equal([2UL, 1UL], chain.Select(b => b.Head.Id));
    }

    [Fact]
    public async Task Quoting_a_continuation_keeps_the_chain_of_its_block_head()
    {
        var blockOf = MessageBlock.Partition(Scenario(personBRepliesTo: 2).Take(3));
        var quoted = blockOf[3];
        var chain = await MessageBlock.WalkChainAsync(quoted, blockOf, 10, NoFetch);

        Assert.Equal(2UL, quoted.Head.Id);
        Assert.True(quoted.Contains(3));
        Assert.Equal([1UL], chain.Select(b => b.Head.Id));
    }

    private static IEnumerable<(ulong Id, bool StartsHeader)> Headers(MessageBlock block)
        => block.Flattened().Select(f => (f.Message.Id, f.StartsHeader));

    [Fact]
    public void A_pause_of_exactly_seven_minutes_keeps_the_turn_but_starts_a_new_header()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, PersonA, 0),
            Msg(2, Hime, 60, replyTo: 1),
            Msg(3, Hime, 60 + 7 * 60),
        ]);

        Assert.Same(blockOf[2], blockOf[3]);
        Assert.Equal([(2UL, true), (3UL, true)], Headers(blockOf[2]));
    }

    [Fact]
    public void A_pause_just_under_seven_minutes_stays_under_the_same_header()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, PersonA, 0),
            Msg(2, Hime, 60, replyTo: 1),
            Msg(3, Hime, 60 + 7 * 60 - 1),
        ]);

        Assert.Same(blockOf[2], blockOf[3]);
        Assert.Equal([(2UL, true), (3UL, false)], Headers(blockOf[2]));
    }

    [Fact]
    public void Header_gap_is_measured_from_the_current_header_not_the_previous_message()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, Hime, 0),
            Msg(2, Hime, 6 * 60),
            Msg(3, Hime, 12 * 60),
        ]);

        // Measured from the previous message every gap is 6 minutes and nothing would split.
        Assert.Equal([(1UL, true), (2UL, false), (3UL, true)], Headers(blockOf[1]));
    }

    [Fact]
    public void Another_author_in_between_ends_the_turn_even_within_seven_minutes()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, Hime, 0),
            Msg(2, PersonB, 10),
            Msg(3, Hime, 20),
        ]);

        Assert.NotSame(blockOf[1], blockOf[3]);
    }

    [Fact]
    public void A_message_never_groups_under_a_system_message()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, Hime, 0, type: MessageType.GuildMemberJoin),
            Msg(2, Hime, 30),
        ]);

        Assert.NotSame(blockOf[1], blockOf[2]);
    }

    [Fact]
    public void A_message_with_a_thread_starts_a_new_block()
    {
        var withThread = Msg(2, Hime, 30);
        Mock.Get((IUserMessage) withThread).SetupGet(m => m.Flags).Returns(MessageFlags.HasThread);

        var blockOf = MessageBlock.Partition([Msg(1, Hime, 0), withThread]);

        Assert.NotSame(blockOf[1], blockOf[2]);
    }

    [Fact]
    public void A_reply_by_the_same_author_always_starts_a_new_block()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, PersonA, 0),
            Msg(2, Hime, 60, replyTo: 1),
            Msg(3, Hime, 61, replyTo: 1),
        ]);

        Assert.NotSame(blockOf[2], blockOf[3]);
        MessageBlock.LinkChildren(blockOf, [1]);
        Assert.Equal([blockOf[2], blockOf[3]], blockOf[1].Children);
    }

    [Fact]
    public void Another_author_in_between_breaks_the_group()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, PersonA, 0),
            Msg(2, Hime, 60, replyTo: 1),
            Msg(3, PersonB, 61),
            Msg(4, Hime, 62),
        ]);

        Assert.NotSame(blockOf[2], blockOf[4]);
        Assert.Null(blockOf[4].ParentId);
    }

    [Fact]
    public void System_messages_never_link_as_replies()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, PersonA, 0),
            Msg(2, Hime, 60, replyTo: 1, type: MessageType.ChannelPinnedMessage),
        ]);

        Assert.Null(blockOf[2].ParentId);
        MessageBlock.LinkChildren(blockOf, [1]);
        Assert.Empty(blockOf[1].Children);
    }

    [Fact]
    public async Task A_forward_is_not_a_reply()
    {
        var forward = Msg(2, Hime, 30, type: MessageType.Default);
        Mock.Get((IUserMessage) forward).SetupGet(m => m.Reference)
            .Returns(new MessageReference(1, referenceType: MessageReferenceType.Forward));
        var blockOf = MessageBlock.Partition([Msg(1, PersonA, 0), forward]);

        Assert.Null(blockOf[2].ParentId);
        Assert.Empty(await MessageBlock.WalkChainAsync(blockOf[2], blockOf, 10, NoFetch));
        MessageBlock.LinkChildren(blockOf, [1]);
        Assert.Empty(blockOf[1].Children);
    }

    [Fact]
    public void A_forward_by_the_same_author_groups_like_any_other_message()
    {
        var forward = Msg(2, Hime, 30, type: MessageType.Default);
        Mock.Get((IUserMessage) forward).SetupGet(m => m.Reference)
            .Returns(new MessageReference(1, referenceType: MessageReferenceType.Forward));
        var blockOf = MessageBlock.Partition([Msg(1, Hime, 0), forward]);

        Assert.Same(blockOf[1], blockOf[2]);
    }

    [Fact]
    public void Webhook_messages_with_different_display_names_do_not_group()
    {
        var alice = Msg(1, 900, 0);
        var bob = Msg(2, 900, 20);
        foreach (var (message, name) in new[] { (alice, "Alice"), (bob, "Bob") })
        {
            var author = Mock.Get(message.Author);
            author.SetupGet(u => u.IsWebhook).Returns(true);
            author.SetupGet(u => u.Username).Returns(name);
        }

        var blockOf = MessageBlock.Partition([alice, bob]);

        Assert.NotSame(blockOf[1], blockOf[2]);
    }

    [Fact]
    public async Task Trunk_blocks_sort_after_sibling_replies_even_when_older()
    {
        var blockOf = MessageBlock.Partition(
        [
            Msg(1, PersonA, 0),
            Msg(2, Hime, 60, replyTo: 1),
            Msg(3, PersonB, 90, replyTo: 1),
            Msg(4, PersonB, 120, replyTo: 2),
        ]);
        var quoted = blockOf[4];
        var chain = await MessageBlock.WalkChainAsync(quoted, blockOf, 10, NoFetch);
        MessageBlock.LinkChildren(blockOf, chain.Select(b => b.Head.Id).Append(quoted.Head.Id).ToHashSet());

        Assert.Equal([2UL, 1UL], chain.Select(b => b.Head.Id));
        Assert.Equal([blockOf[3], blockOf[2]], blockOf[1].Children);
    }

    [Fact]
    public async Task Cached_referenced_message_is_used_without_fetching()
    {
        var root = Msg(1, PersonA, 0);
        var reply = Msg(2, Hime, 60, replyTo: 1);
        Mock.Get((IUserMessage) reply).SetupGet(m => m.ReferencedMessage).Returns((IUserMessage) root);
        var blockOf = MessageBlock.Partition([reply]);

        var chain = await MessageBlock.WalkChainAsync(blockOf[2], blockOf, 10,
            _ => throw new InvalidOperationException("fetch must not be called"));

        Assert.Equal([1UL], chain.Select(b => b.Head.Id));
    }

    [Fact]
    public void Partition_ignores_input_order_and_duplicates()
    {
        var messages = Scenario(personBRepliesTo: 2);
        var shuffled = messages.AsEnumerable().Reverse().Concat(messages).ToList();

        var blockOf = MessageBlock.Partition(shuffled);

        Assert.Equal(4, blockOf.Values.Distinct().Count());
        Assert.Same(blockOf[2], blockOf[3]);
        Assert.Equal([2UL, 3UL], blockOf[2].Messages.Select(m => m.Id));
    }

    [Fact]
    public async Task Parent_outside_the_window_is_fetched_as_a_single_message_block()
    {
        var root = Msg(1, PersonA, 0);
        var blockOf = MessageBlock.Partition([Msg(2, Hime, 60, replyTo: 1)]);

        var chain = await MessageBlock.WalkChainAsync(blockOf[2], blockOf, 10,
            id => Task.FromResult<IMessage?>(id == 1 ? root : null));

        Assert.Equal([1UL], chain.Select(b => b.Head.Id));
        Assert.Same(chain[0], blockOf[1]);
    }

    [Fact]
    public async Task Missing_parent_ends_the_chain()
    {
        var blockOf = MessageBlock.Partition([Msg(2, Hime, 60, replyTo: 1)]);

        var chain = await MessageBlock.WalkChainAsync(blockOf[2], blockOf, 10, NoFetch);

        Assert.Empty(chain);
    }

    [Fact]
    public async Task Chain_depth_is_capped()
    {
        var messages = Enumerable.Range(1, 15)
            .Select(i => Msg((ulong) i, (ulong) i, i, replyTo: i == 1 ? null : (ulong) (i - 1)))
            .ToList();
        var blockOf = MessageBlock.Partition(messages);

        var chain = await MessageBlock.WalkChainAsync(blockOf[15], blockOf, 10, NoFetch);

        Assert.Equal(10, chain.Count);
        Assert.Equal(14UL, chain[0].Head.Id);
    }
}
