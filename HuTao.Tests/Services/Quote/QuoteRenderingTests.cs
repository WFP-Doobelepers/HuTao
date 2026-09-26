using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using HuTao.Services.Quote;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Quote;

public class QuoteRenderingTests
{
    private const ulong PersonA = 100;
    private const ulong Hime = 200;
    private const ulong PersonB = 300;

    private const ulong Aria = 100;
    private const ulong Blaise = 200;
    private const ulong Cyra = 300;

    private static readonly DateTimeOffset T0 = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    private static Mock<IUserMessage> Msg(
        IMessageChannel channel, ulong id, ulong author, int secondsAfterT0, string content, ulong? replyTo = null,
        IAttachment[]? attachments = null)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(author);
        user.SetupGet(u => u.Mention).Returns($"<@{author}>");

        var message = new Mock<IUserMessage>();
        message.SetupGet(m => m.Id).Returns(id);
        message.SetupGet(m => m.Channel).Returns(channel);
        message.SetupGet(m => m.Author).Returns(user.Object);
        message.SetupGet(m => m.Timestamp).Returns(T0.AddSeconds(secondsAfterT0));
        message.SetupGet(m => m.Type).Returns(replyTo is null ? MessageType.Default : MessageType.Reply);
        message.SetupGet(m => m.Reference).Returns(replyTo is null ? null! : new MessageReference(replyTo));
        message.SetupGet(m => m.Content).Returns(content);
        message.SetupGet(m => m.Attachments).Returns(attachments ?? []);
        message.SetupGet(m => m.Embeds).Returns(Array.Empty<IEmbed>());
        message.SetupGet(m => m.Components).Returns(Array.Empty<IMessageComponent>());
        return message;
    }

    private static IAttachment Image(string url)
    {
        var attachment = new Mock<IAttachment>();
        attachment.SetupGet(a => a.Url).Returns(url);
        attachment.SetupGet(a => a.ProxyUrl).Returns(url);
        attachment.SetupGet(a => a.ContentType).Returns("image/png");
        attachment.SetupGet(a => a.Filename).Returns("pic.png");
        attachment.SetupGet(a => a.Height).Returns(10);
        attachment.SetupGet(a => a.Width).Returns(10);
        return attachment.Object;
    }

    private static async IAsyncEnumerable<IReadOnlyCollection<IMessage>> Page(IReadOnlyCollection<IMessage> page)
    {
        yield return page;
        await Task.CompletedTask;
    }

    /// <summary>Serves <paramref name="history" /> (oldest first) as the page before <paramref name="quotedId" />.</summary>
    private static void History(Mock<IMessageChannel> channel, ulong quotedId, params Mock<IUserMessage>[] history)
        => channel
            .Setup(c => c.GetMessagesAsync(quotedId, Direction.Before, It.IsAny<int>(), It.IsAny<CacheMode>(),
                It.IsAny<RequestOptions>()))
            .Returns(Page(history.Select(m => (IMessage) m.Object).Reverse().ToList()));

    private static string Text(IEnumerable<ContainerBuilder> containers)
        => string.Join("\n", containers
            .SelectMany(c => c.Components)
            .OfType<TextDisplayBuilder>()
            .Select(t => t.Content));

    /// <summary>
    ///     Replaces the connector emoji with box characters and timestamps with &lt;t&gt; so assertions stay readable.
    ///     Line endings are normalized because the accumulator uses <see cref="Environment.NewLine" />.
    /// </summary>
    private static string Sym(string text) => Regex.Replace(text
            .ReplaceLineEndings("\n")
            .Replace("<:reply_right:1479788099457519758>", "┌")
            .Replace("<:reply:1479788090942820476>", "└")
            .Replace("<:reply_t:1479788095183261880>", "├")
            .Replace("<:reply_line:1479788104394080326>", "│")
            .Replace("<:reply_spacer:1479788137512177716>", "·"),
        @"<t:\d+:R>", "<t>");

    /// <summary>Renders the quote and checks that every line connects.</summary>
    private static async Task<string> Render(IUserMessage quoted, bool expanded = false)
    {
        var containers = await QuoteService.BuildMessageContainer(quoted, expanded);
        AssertLinesConnect(containers);
        return Sym(Text(containers));
    }

    /// <summary>
    ///     Checks each text display on its own. An image or the end of a container sits between two text displays, so
    ///     a rail may stop at the bottom of one and continue at the top of the next. The top of the quote and the
    ///     bottom of the quote must still be closed.
    /// </summary>
    private static void AssertLinesConnect(IEnumerable<ContainerBuilder> containers)
    {
        var displays = containers
            .SelectMany(c => c.Components)
            .OfType<TextDisplayBuilder>()
            .Select(t => Sym(t.Content))
            .ToList();

        for (var i = 0; i < displays.Count; i++)
            AssertLinesConnect(displays[i], openTop: i > 0, openBottom: i < displays.Count - 1);
    }

    /// <summary>
    ///     Every connector must meet another line. A line going up must reach a line, or hang from the text right above
    ///     it (a new rail, exactly one column right of its parent's rail). A line going down must reach a line, or end
    ///     in the header of the message that sits at the end of the line.
    /// </summary>
    private static void AssertLinesConnect(string sym, bool openTop = false, bool openBottom = false)
    {
        const string up = "├└│";
        const string down = "┌├│";
        const string gutter = "┌└├│·";

        var lines = sym.Split('\n')
            .Select(l => l == "-#" ? "" : l.StartsWith("-# ") ? l[3..] : l)
            .ToArray();

        for (var r = 0; r < lines.Length; r++)
        {
            for (var p = 0; p < GutterLength(r); p++)
            {
                var ch = lines[r][p];

                if (up.Contains(ch) && !(openTop && r == 0))
                {
                    var hangsFromText = r > 0 && p == GutterLength(r - 1) && lines[r - 1].Length > GutterLength(r - 1);
                    Assert.True(down.Contains(At(r - 1, p)) || hangsFromText,
                        $"Line {r + 1}: the {ch} in column {p} has nothing above it.\n{sym}");
                }

                if (down.Contains(ch) && !(openBottom && r == lines.Length - 1))
                {
                    var endsInHeader = r + 1 < lines.Length && lines[r + 1].Length > p
                        && lines[r + 1][p..].StartsWith("<@");
                    Assert.True(up.Contains(At(r + 1, p)) || endsInHeader,
                        $"Line {r + 1}: the {ch} in column {p} stops, nothing below it.\n{sym}");
                }
            }
        }

        int GutterLength(int r) => lines[r].TakeWhile(gutter.Contains).Count();

        char At(int r, int p) => r >= 0 && r < lines.Length && p < lines[r].Length ? lines[r][p] : ' ';
    }

    /// <summary>
    ///     person A: something / hime-san (reply): A / hime-san: B / person B: Good morning! /
    ///     person B (reply to <paramref name="personBRepliesTo" />): C  ← quoted
    /// </summary>
    private static IUserMessage Scenario(ulong personBRepliesTo)
    {
        var channel = new Mock<IMessageChannel>();
        var a = Msg(channel.Object, 1, PersonA, 0, "something");
        var h1 = Msg(channel.Object, 2, Hime, 60, "I see, if that's the case we should do that", replyTo: 1);
        var h2 = Msg(channel.Object, 3, Hime, 90, "And then we should also do this");
        var b1 = Msg(channel.Object, 4, PersonB, 120, "Good morning!");
        var b2 = Msg(channel.Object, 5, PersonB, 150, "Ohh really? This is interesting!", replyTo: personBRepliesTo);
        History(channel, 5, a, h1, h2, b1);
        return b2.Object;
    }

    /// <summary>
    ///     A root with two branches. The "agreed" branch is a straight chain down to "ok, that is convincing". The
    ///     "what about the lexer?" branch forks: two replies, the second with a chain of its own. Cyra's messages
    ///     reply to nothing.
    /// </summary>
    private static IUserMessage Quote(ulong id)
    {
        var channel = new Mock<IMessageChannel>();
        var c = channel.Object;
        Mock<IUserMessage>[] all =
        [
            Msg(c, 1, Aria, 0, "the parser is too slow"),
            Msg(c, 2, Aria, 1, "mostly in the tokenizer"),
            Msg(c, 3, Blaise, 3, "agreed", replyTo: 1),
            Msg(c, 4, Blaise, 4, "it allocates on every call"),
            Msg(c, 5, Blaise, 6, "and it re-scans the same span twice"),
            Msg(c, 6, Aria, 7, "did you profile it?", replyTo: 5),
            Msg(c, 7, Cyra, 9, "good morning everyone"),
            Msg(c, 8, Cyra, 10, "anyone up for lunch later?"),
            Msg(c, 10, Blaise, 14, "yes, forty percent of runtime", replyTo: 6),
            Msg(c, 11, Aria, 16, "numbers or it did not happen", replyTo: 10),
            Msg(c, 12, Blaise, 18, "the allocation shows up in three places", replyTo: 11),
            Msg(c, 13, Aria, 20, "ok, that is convincing", replyTo: 12),
            Msg(c, 14, Blaise, 25, "what about the lexer?", replyTo: 1),
            Msg(c, 15, Aria, 26, "the lexer is fine", replyTo: 14),
            Msg(c, 16, Blaise, 28, "two separate questions here", replyTo: 14),
            Msg(c, 17, Aria, 29, "here is the flame graph", replyTo: 16),
            Msg(c, 18, Aria, 32, "and here is the fix"),
            Msg(c, 19, Blaise, 33, "nice, ship it", replyTo: 18),
            Msg(c, 20, Blaise, 40, "great, let's ship the fix", replyTo: 13)
        ];

        History(channel, id, all.Where(m => m.Object.Id < id).ToArray());
        return all.Single(m => m.Object.Id == id).Object;
    }

    [Fact]
    public async Task Collapsed_quote_merges_same_author_continuation_and_skips_unrelated_messages()
    {
        var text = await Render(Scenario(personBRepliesTo: 2));

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            -# ├ <@200> · <t>
            -# │ I see, if that's the case we should do that
            -# │ And then we should also do this
            -# │
            <@300> · <t>
            Ohh really? This is interesting!
            """.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public async Task Collapsed_quote_of_a_reply_to_the_root_skips_the_side_branch()
    {
        var text = await Render(Scenario(personBRepliesTo: 1));

        Assert.DoesNotContain($"<@{Hime}>", text);
        Assert.DoesNotContain("Good morning!", text);
        Assert.Contains("something", text);
        Assert.Contains("Ohh really?", text);
    }

    [Fact]
    public async Task Expanded_quote_of_a_straight_chain_is_the_collapsed_picture()
    {
        Assert.Equal(
            await Render(Scenario(personBRepliesTo: 2)),
            await Render(Scenario(personBRepliesTo: 2), expanded: true));
    }

    /// <summary>A reply to the root and a reply to hime both stay on the one rail, so they look the same.</summary>
    [Fact]
    public async Task Expanded_quote_keeps_a_reply_to_the_root_on_the_same_rail_as_hime()
    {
        var text = await Render(Scenario(personBRepliesTo: 1), expanded: true);

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            -# ├ <@200> · <t>
            -# │ I see, if that's the case we should do that
            -# │ And then we should also do this
            -# │
            <@300> · <t>
            Ohh really? This is interesting!
            """.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public async Task Expanded_quote_of_a_reply_to_the_continuation_matches_a_reply_to_hime()
    {
        Assert.Equal(
            await Render(Scenario(personBRepliesTo: 2), expanded: true),
            await Render(Scenario(personBRepliesTo: 3), expanded: true));
    }

    [Fact]
    public async Task Quoting_a_continuation_shows_the_whole_block_under_one_header()
    {
        var channel = new Mock<IMessageChannel>();
        var a = Msg(channel.Object, 1, PersonA, 0, "something");
        var h1 = Msg(channel.Object, 2, Hime, 60, "A", replyTo: 1);
        var h2 = Msg(channel.Object, 3, Hime, 90, "B");
        var h3 = Msg(channel.Object, 4, Hime, 120, "C");
        History(channel, 4, a, h1, h2);

        const string expected = """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            <@200> · <t>
            -# A
            -# B
            C
            """;

        Assert.Equal(expected.ReplaceLineEndings("\n"), await Render(h3.Object));
        Assert.Equal(expected.ReplaceLineEndings("\n"), await Render(h3.Object, expanded: true));
    }

    /// <summary>
    ///     Same author, nobody in between, but seven minutes later: Discord draws a second header and still shows
    ///     the message, so the quote must too.
    /// </summary>
    [Fact]
    public async Task A_long_pause_gives_the_late_message_its_own_header_instead_of_dropping_it()
    {
        var channel = new Mock<IMessageChannel>();
        var a = Msg(channel.Object, 1, PersonA, 0, "something");
        var h1 = Msg(channel.Object, 2, Hime, 60, "I see, if that's the case we should do that", replyTo: 1);
        var h2 = Msg(channel.Object, 3, Hime, 60 + 7 * 60, "And then we should also do this");
        var quoted = Msg(channel.Object, 4, PersonB, 60 + 8 * 60, "Ohh really? This is interesting!", replyTo: 2);
        History(channel, 4, a, h1, h2);

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            -# ├ <@200> · <t>
            -# │ I see, if that's the case we should do that
            -# │
            -# │ <@200> · <t>
            -# │ And then we should also do this
            -# │
            <@300> · <t>
            Ohh really? This is interesting!
            """.ReplaceLineEndings("\n"), await Render(quoted.Object));
    }

    [Fact]
    public async Task A_long_pause_inside_the_quoted_message_own_turn_also_gets_its_own_header()
    {
        var channel = new Mock<IMessageChannel>();
        var a = Msg(channel.Object, 1, PersonA, 0, "something");
        var h1 = Msg(channel.Object, 2, Hime, 60, "I see, if that's the case we should do that", replyTo: 1);
        var h2 = Msg(channel.Object, 3, Hime, 60 + 7 * 60, "And then we should also do this");
        History(channel, 3, a, h1);

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            <@200> · <t>
            -# I see, if that's the case we should do that
            -#
            -# <@200> · <t>
            And then we should also do this
            """.ReplaceLineEndings("\n"), await Render(h2.Object));
    }

    /// <summary>
    ///     The "agreed" branch has a sibling after it on the main rail, so its replies move one column right. The
    ///     lexer branch leads to the quoted message, so it stays on the main rail, and its two replies to "what about
    ///     the lexer?" sit on that rail too.
    /// </summary>
    [Fact]
    public async Task Expanded_quote_keeps_chains_flat_and_nests_only_under_a_turn_with_a_sibling()
    {
        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ the parser is too slow
            -# │ mostly in the tokenizer
            -# │
            -# ├ <@200> · <t>
            -# │ agreed
            -# │ it allocates on every call
            -# │ and it re-scans the same span twice
            -# ││
            -# │├ <@100> · <t>
            -# ││ did you profile it?
            -# ││
            -# │├ <@200> · <t>
            -# ││ yes, forty percent of runtime
            -# ││
            -# │├ <@100> · <t>
            -# ││ numbers or it did not happen
            -# ││
            -# │├ <@200> · <t>
            -# ││ the allocation shows up in three places
            -# ││
            -# │└ <@100> · <t>
            -# │· ok, that is convincing
            -# │
            -# ├ <@200> · <t>
            -# │ what about the lexer?
            -# │
            -# ├ <@100> · <t>
            -# │ the lexer is fine
            -# │
            -# ├ <@200> · <t>
            -# │ two separate questions here
            -# │
            -# ├ <@100> · <t>
            -# │ here is the flame graph
            -# │ and here is the fix
            -# │
            <@200> · <t>
            nice, ship it
            """.ReplaceLineEndings("\n"), await Render(Quote(19), expanded: true));
    }

    /// <summary>
    ///     Here the lexer branch is the side branch. "two separate questions here" has a sibling, so its own chain
    ///     moves one more column right, under it.
    /// </summary>
    [Fact]
    public async Task Expanded_quote_nests_the_replies_of_every_turn_that_has_a_sibling()
    {
        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ the parser is too slow
            -# │ mostly in the tokenizer
            -# │
            -# ├ <@200> · <t>
            -# │ what about the lexer?
            -# ││
            -# │├ <@100> · <t>
            -# ││ the lexer is fine
            -# ││
            -# │└ <@200> · <t>
            -# │· two separate questions here
            -# │·│
            -# │·├ <@100> · <t>
            -# │·│ here is the flame graph
            -# │·│ and here is the fix
            -# │·│
            -# │·└ <@200> · <t>
            -# │·· nice, ship it
            -# │
            -# ├ <@200> · <t>
            -# │ agreed
            -# │ it allocates on every call
            -# │ and it re-scans the same span twice
            -# │
            -# ├ <@100> · <t>
            -# │ did you profile it?
            -# │
            -# ├ <@200> · <t>
            -# │ yes, forty percent of runtime
            -# │
            -# ├ <@100> · <t>
            -# │ numbers or it did not happen
            -# │
            -# ├ <@200> · <t>
            -# │ the allocation shows up in three places
            -# │
            -# ├ <@100> · <t>
            -# │ ok, that is convincing
            -# │
            <@200> · <t>
            great, let's ship the fix
            """.ReplaceLineEndings("\n"), await Render(Quote(20), expanded: true));
    }

    [Fact]
    public async Task Collapsed_quote_is_the_expanded_picture_without_the_side_branches()
    {
        var collapsed = await Render(Quote(20));
        var expanded = await Render(Quote(20), expanded: true);

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ the parser is too slow
            -# │ mostly in the tokenizer
            -# │
            -# ├ <@200> · <t>
            -# │ agreed
            -# │ it allocates on every call
            -# │ and it re-scans the same span twice
            -# │
            -# ├ <@100> · <t>
            -# │ did you profile it?
            -# │
            -# ├ <@200> · <t>
            -# │ yes, forty percent of runtime
            -# │
            -# ├ <@100> · <t>
            -# │ numbers or it did not happen
            -# │
            -# ├ <@200> · <t>
            -# │ the allocation shows up in three places
            -# │
            -# ├ <@100> · <t>
            -# │ ok, that is convincing
            -# │
            <@200> · <t>
            great, let's ship the fix
            """.ReplaceLineEndings("\n"), collapsed);

        var lines = collapsed.Split('\n');
        Assert.StartsWith(string.Join("\n", lines.Take(4)) + "\n", expanded);
        Assert.EndsWith("\n" + string.Join("\n", lines.Skip(4)), expanded);
    }

    [Fact]
    public async Task Collapsed_quote_follows_the_reply_chain_through_a_fork()
    {
        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ the parser is too slow
            -# │ mostly in the tokenizer
            -# │
            -# ├ <@200> · <t>
            -# │ what about the lexer?
            -# │
            -# ├ <@200> · <t>
            -# │ two separate questions here
            -# │
            -# ├ <@100> · <t>
            -# │ here is the flame graph
            -# │ and here is the fix
            -# │
            <@200> · <t>
            nice, ship it
            """.ReplaceLineEndings("\n"), await Render(Quote(19)));
    }

    /// <summary>The fork example in paragraph 7.2 of the quote README.</summary>
    [Fact]
    public async Task Readme_fork_example()
    {
        var channel = new Mock<IMessageChannel>();
        var c = channel.Object;
        var m1 = Msg(c, 1, 100, 0, "something");
        var m2 = Msg(c, 2, 200, 60, "I see", replyTo: 1);
        var m3 = Msg(c, 3, 300, 120, "Good point", replyTo: 1);
        var m4 = Msg(c, 4, 400, 150, "Agreed", replyTo: 3);
        var m5 = Msg(c, 5, 500, 180, "Ohh really?", replyTo: 1);
        History(channel, 5, m1, m2, m3, m4);

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            -# ├ <@200> · <t>
            -# │ I see
            -# │
            -# ├ <@300> · <t>
            -# │ Good point
            -# ││
            -# │└ <@400> · <t>
            -# │· Agreed
            -# │
            <@500> · <t>
            Ohh really?
            """.ReplaceLineEndings("\n"), await Render(m5.Object, expanded: true));

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            <@500> · <t>
            Ohh really?
            """.ReplaceLineEndings("\n"), await Render(m5.Object));
    }

    /// <summary>The history window ends at the quoted message, so replies sent after it never appear.</summary>
    [Fact]
    public async Task Expanded_quote_only_reads_messages_sent_before_the_quoted_one()
    {
        var expanded = await Render(Quote(13), expanded: true);

        Assert.DoesNotContain("what about the lexer?", expanded);
        Assert.Equal(await Render(Quote(13)), expanded);
    }

    [Theory]
    [InlineData(13UL)]
    [InlineData(19UL)]
    [InlineData(20UL)]
    public async Task Messages_that_reply_to_nothing_never_appear(ulong quoted)
    {
        foreach (var expanded in new[] { false, true })
        {
            var text = await Render(Quote(quoted), expanded);
            Assert.DoesNotContain("good morning everyone", text);
            Assert.DoesNotContain("anyone up for lunch later?", text);
        }
    }

    /// <summary>
    ///     The breaks the old tree renderer drew (a connector hanging in the air, a rail with no end), and a new rail
    ///     that jumps two columns instead of one.
    /// </summary>
    [Theory]
    [InlineData("-# ┌ <@1> · <t>\n-# │ root\n-# │\n-# │├ <@2> · <t>\n-# ││ reply")]
    [InlineData("-# ┌ <@1> · <t>\n-# │ root\n-# │\n-# ├ <@2> · <t>\n-# │ reply")]
    [InlineData("-# ┌ <@1> · <t>\n-# │ root\n-# │·├ <@2> · <t>\n-# │·│ reply\n-# │·└ <@3> · <t>\n<@4> · <t>")]
    public void Line_checker_rejects_a_broken_rail(string broken)
    {
        Assert.ThrowsAny<Exception>(() => AssertLinesConnect(broken));
    }

    /// <summary>
    ///     A quote too long for one message goes into several containers. A rail cannot cross between them, so each
    ///     container after the first must start at a turn, with the header and its text together.
    /// </summary>
    [Fact]
    public async Task A_quote_too_long_for_one_message_splits_where_a_turn_starts()
    {
        var channel = new Mock<IMessageChannel>();
        var c = channel.Object;
        var text = string.Concat(Enumerable.Repeat("a long line of text that fills the message ", 2));
        var history = new List<Mock<IUserMessage>> { Msg(c, 1, Aria, 0, "the parser is too slow") };
        for (ulong id = 2; id <= 61; id++)
            history.Add(Msg(c, id, id % 2 == 0 ? Blaise : Cyra, (int) id, $"{id}: {text}", replyTo: id - 1));
        var quoted = Msg(c, 62, Aria, 90, "ok", replyTo: 1);
        History(channel, 62, history.ToArray());

        var containers = await QuoteService.BuildMessageContainer(quoted.Object, expanded: true);
        var rendered = await Render(quoted.Object, expanded: true);

        Assert.True(containers.Count > 1);
        foreach (var container in containers.Skip(1))
        {
            var first = Sym(container.Components.OfType<TextDisplayBuilder>().First().Content).Split('\n');
            Assert.Matches(@"^(-# [┌├└│·]+ )?<@\d+> · <t>$", first[0]);
            Assert.DoesNotMatch(@"<@\d+> · <t>$", first[1]);
        }

        for (ulong id = 2; id <= 61; id++)
            Assert.Contains($"{id}: {text.TrimEnd()}", rendered);
    }

    [Fact]
    public async Task Plain_message_without_a_chain_renders_alone()
    {
        var channel = new Mock<IMessageChannel>();
        var earlier = Msg(channel.Object, 1, Hime, 0, "earlier");
        var quoted = Msg(channel.Object, 2, Hime, 30, "hello");
        History(channel, 2, earlier);

        var text = await Render(quoted.Object);

        Assert.Contains("hello", text);
        Assert.DoesNotContain("earlier", text);
    }

    [Fact]
    public async Task History_fetch_failure_still_builds_the_chain_from_fetched_parents()
    {
        var channel = new Mock<IMessageChannel>();
        var root = Msg(channel.Object, 1, PersonA, 0, "root");
        var quoted = Msg(channel.Object, 2, Hime, 60, "quoted", replyTo: 1);
        channel
            .Setup(c => c.GetMessagesAsync(It.IsAny<ulong>(), It.IsAny<Direction>(), It.IsAny<int>(),
                It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .Throws(new HttpException(HttpStatusCode.Forbidden, new Mock<IRequest>().Object));
        channel
            .Setup(c => c.GetMessageAsync(1UL, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(root.Object);

        var text = await Render(quoted.Object);

        Assert.StartsWith("-# ┌ <@100> · <t>\n-# │ root\n", text);
        Assert.Contains("quoted", text);
    }

    [Fact]
    public async Task Parent_fetch_failure_ends_the_chain_instead_of_throwing()
    {
        var channel = new Mock<IMessageChannel>();
        var quoted = Msg(channel.Object, 2, Hime, 60, "quoted", replyTo: 1);
        History(channel, 2);
        channel
            .Setup(c => c.GetMessageAsync(1UL, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ThrowsAsync(new HttpException(HttpStatusCode.Forbidden, new Mock<IRequest>().Object));

        var text = await Render(quoted.Object);

        Assert.Contains("quoted", text);
        Assert.DoesNotContain("┌", text);
    }

    [Fact]
    public async Task Image_inside_a_block_keeps_the_column_for_the_following_text()
    {
        var channel = new Mock<IMessageChannel>();
        var a = Msg(channel.Object, 1, PersonA, 0, "something");
        var h1 = Msg(channel.Object, 2, Hime, 60, "look", replyTo: 1,
            attachments: [Image("https://cdn.discordapp.com/x/pic.png")]);
        var h2 = Msg(channel.Object, 3, Hime, 90, "and this");
        var quoted = Msg(channel.Object, 4, PersonB, 120, "nice", replyTo: 2);
        History(channel, 4, a, h1, h2);

        var containers = await QuoteService.BuildMessageContainer(quoted.Object);
        var components = containers.SelectMany(c => c.Components).ToList();

        Assert.Equal(
            [nameof(TextDisplayBuilder), nameof(MediaGalleryBuilder), nameof(TextDisplayBuilder)],
            components.Select(c => c.GetType().Name));
        Assert.Equal("-# │ and this\n-# │\n<@300> · <t>\nnice", Sym(((TextDisplayBuilder) components[2]).Content));
    }

    [Fact]
    public async Task Image_as_the_last_thing_before_the_quoted_message_resumes_the_line_below_it()
    {
        var channel = new Mock<IMessageChannel>();
        var a = Msg(channel.Object, 1, PersonA, 0, "something");
        var h1 = Msg(channel.Object, 2, Hime, 60, "look", replyTo: 1,
            attachments: [Image("https://cdn.discordapp.com/x/pic.png")]);
        var quoted = Msg(channel.Object, 3, PersonB, 120, "nice", replyTo: 2);
        History(channel, 3, a, h1);

        var text = await Render(quoted.Object);

        Assert.EndsWith("-# │ look\n-# │\n<@300> · <t>\nnice", text);
    }
}
