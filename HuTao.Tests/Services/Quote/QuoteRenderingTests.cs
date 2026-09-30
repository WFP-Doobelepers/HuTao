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

    /// <summary>All text in drawing order, including the text inside a section (the "truncated" marker).</summary>
    private static string Text(IEnumerable<ContainerBuilder> containers)
        => string.Join("\n", containers.SelectMany(c => c.Components).SelectMany(TextOf));

    private static IEnumerable<string> TextOf(IMessageComponentBuilder component) => component switch
    {
        TextDisplayBuilder text => [text.Content],
        SectionBuilder section  => section.Components.SelectMany(TextOf),
        _                       => []
    };

    /// <summary>The text length Discord counts against its limit.</summary>
    private static int TextLength(IEnumerable<ContainerBuilder> containers)
        => containers.SelectMany(c => c.Components).SelectMany(TextOf).Sum(t => t.Length);


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

    /// <summary>
    ///     "Good morning!" replies to nothing. Collapsed mode leaves it out; expanded mode shows it right after the
    ///     turn sent before it, with its own header and no connector.
    /// </summary>
    [Fact]
    public async Task Expanded_quote_shows_a_message_that_replies_to_nothing_without_a_connector()
    {
        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            -# ├ <@200> · <t>
            -# │ I see, if that's the case we should do that
            -# │ And then we should also do this
            -# │
            -# │ <@300> · <t>
            -# │ Good morning!
            -# │
            <@300> · <t>
            Ohh really? This is interesting!
            """.ReplaceLineEndings("\n"), await Render(Scenario(personBRepliesTo: 2), expanded: true));
    }

    /// <summary>A reply to the root and a reply to hime both stay on the one rail, so they look the same.</summary>
    [Fact]
    public async Task Expanded_quote_draws_a_reply_to_the_root_like_a_reply_to_hime()
    {
        Assert.Equal(
            await Render(Scenario(personBRepliesTo: 2), expanded: true),
            await Render(Scenario(personBRepliesTo: 1), expanded: true));
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
            -# ││ <@300> · <t>
            -# ││ good morning everyone
            -# ││ anyone up for lunch later?
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
            -# │ <@300> · <t>
            -# │ good morning everyone
            -# │ anyone up for lunch later?
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
    public async Task Collapsed_quote_is_the_expanded_picture_without_the_side_branches_and_loose_turns()
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

        const string cyra = "-# │\n-# │ <@300> · <t>\n-# │ good morning everyone\n-# │ anyone up for lunch later?\n";
        Assert.Contains(cyra, expanded);

        var lines = collapsed.Split('\n');
        var withoutLoose = expanded.Replace(cyra, "");
        Assert.StartsWith(string.Join("\n", lines.Take(4)) + "\n", withoutLoose);
        Assert.EndsWith("\n" + string.Join("\n", lines.Skip(4)), withoutLoose);
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

    /// <summary>
    ///     The history window ends at the quoted message, so the lexer branch (sent later) never appears. Cyra's
    ///     messages were sent before it, so they show, unconnected.
    /// </summary>
    [Fact]
    public async Task Expanded_quote_only_reads_messages_sent_before_the_quoted_one()
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
            -# │
            -# ├ <@100> · <t>
            -# │ did you profile it?
            -# │
            -# │ <@300> · <t>
            -# │ good morning everyone
            -# │ anyone up for lunch later?
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
            <@100> · <t>
            ok, that is convincing
            """.ReplaceLineEndings("\n"), await Render(Quote(13), expanded: true));
    }

    [Theory]
    [InlineData(13UL)]
    [InlineData(19UL)]
    [InlineData(20UL)]
    public async Task Messages_that_reply_to_nothing_stay_out_of_collapsed_quotes(ulong quoted)
    {
        var text = await Render(Quote(quoted));

        Assert.DoesNotContain("good morning everyone", text);
        Assert.DoesNotContain("anyone up for lunch later?", text);
    }

    /// <summary>
    ///     Loose turns show only from the root on. A bot's post stays out, so the bot's own earlier quotes never appear
    ///     inside a new one; a webhook's post shows, under the display name it was sent with.
    /// </summary>
    [Fact]
    public async Task Expanded_quote_shows_loose_webhook_posts_but_not_bot_posts_or_earlier_chat()
    {
        var channel = new Mock<IMessageChannel>();
        var c = channel.Object;
        var earlier = Msg(c, 1, 300, 0, "earlier chat");
        var root = Msg(c, 2, 100, 10, "root");
        var bot = Msg(c, 3, 500, 20, "a quote the bot posted");
        Mock.Get(bot.Object.Author).SetupGet(u => u.IsBot).Returns(true);
        var webhook = Msg(c, 4, 400, 30, "webhook chat");
        Mock.Get(webhook.Object.Author).SetupGet(u => u.IsBot).Returns(true);
        Mock.Get(webhook.Object.Author).SetupGet(u => u.IsWebhook).Returns(true);
        Mock.Get(webhook.Object.Author).SetupGet(u => u.Username).Returns("Cyra");
        var quoted = Msg(c, 5, 200, 40, "quoted", replyTo: 2);
        History(channel, 5, earlier, root, bot, webhook);

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ root
            -# │
            -# │ **Cyra** · <t>
            -# │ webhook chat
            -# │
            <@200> · <t>
            quoted
            """.ReplaceLineEndings("\n"), await Render(quoted.Object, expanded: true));
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

    /// <summary>A root, a 60-turn side branch, and a quoted reply to the root: far too long for one message.</summary>
    private static (IUserMessage Quoted, string LineText) LongConversation()
    {
        var channel = new Mock<IMessageChannel>();
        var c = channel.Object;
        var text = string.Concat(Enumerable.Repeat("a long line of text that fills the message ", 2)).TrimEnd();
        var history = new List<Mock<IUserMessage>> { Msg(c, 1, Aria, 0, "the parser is too slow") };
        for (ulong id = 2; id <= 61; id++)
            history.Add(Msg(c, id, id % 2 == 0 ? Blaise : Cyra, (int) id, $"{id}: {text}", replyTo: id - 1));
        var quoted = Msg(c, 62, Aria, 90, "ok", replyTo: 1);
        History(channel, 62, history.ToArray());
        return (quoted.Object, text);
    }

    /// <summary>
    ///     "Show all" leaves nothing out, so a long quote takes several containers. A rail cannot cross between them,
    ///     so each container after the first must start at a turn, with the header and its text together.
    /// </summary>
    [Fact]
    public async Task Show_all_splits_a_long_quote_where_a_turn_starts()
    {
        var (quoted, text) = LongConversation();

        var containers = await QuoteService.BuildMessageContainer(quoted, expanded: true, oneMessage: false);
        AssertLinesConnect(containers);
        var rendered = Sym(Text(containers));

        Assert.True(containers.Count > 1);
        foreach (var container in containers.Skip(1))
        {
            var first = Sym(container.Components.OfType<TextDisplayBuilder>().First().Content).Split('\n');
            Assert.Matches(@"^(-# [┌├└│·]+ )?<@\d+> · <t>$", first[0]);
            Assert.DoesNotMatch(@"<@\d+> · <t>$", first[1]);
        }

        for (ulong id = 2; id <= 61; id++)
            Assert.Contains($"{id}: {text}", rendered);
        Assert.DoesNotContain("truncated", rendered);
    }

    /// <summary>
    ///     The posted quote always fits in one message. The root and the quoted turn stay, and the middle is hidden
    ///     behind one "truncated" line between two separators.
    /// </summary>
    [Fact]
    public async Task A_long_quote_hides_its_middle_to_fit_in_one_message()
    {
        var (quoted, text) = LongConversation();

        var built = await QuoteService.BuildQuote(quoted, expanded: true);
        var rendered = await Render(quoted, expanded: true);

        Assert.True(built.Truncated);
        Assert.Single(built.Containers);
        Assert.True(TextLength(built.Containers) <= 3800);
        Assert.StartsWith("-# ┌ <@100> · <t>\n-# │ the parser is too slow\n", rendered);
        Assert.EndsWith("<@100> · <t>\nok", rendered);
        Assert.Contains($"2: {text}", rendered);
        Assert.Contains($"61: {text}", rendered);
        Assert.Matches(@"\n-# \d+ messages truncated\n", rendered);
        Assert.DoesNotContain($"31: {text}", rendered);
    }

    /// <summary>Hiding one turn is enough here, and the turn hidden is the one in the middle.</summary>
    [Fact]
    public async Task A_quote_slightly_too_long_hides_only_the_middle_turn()
    {
        var full = TextLength(await QuoteService.BuildMessageContainer(Quote(20)));
        var built = await QuoteService.BuildQuote(Quote(20), budget: full - 1);
        AssertLinesConnect(built.Containers);

        Assert.True(built.Truncated);
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
            -# 1 message truncated
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
            """.ReplaceLineEndings("\n"), Sym(Text(built.Containers)));
    }

    /// <summary>
    ///     Unrelated turns go first, before any turn of the conversation is hidden. The "truncated" line stands
    ///     between two separators, exactly where Cyra's messages were.
    /// </summary>
    [Fact]
    public async Task A_quote_too_long_drops_unrelated_messages_first_and_marks_the_spot()
    {
        var full = TextLength(await QuoteService.BuildMessageContainer(Quote(19), expanded: true));
        var built = await QuoteService.BuildQuote(Quote(19), expanded: true, budget: full - 1);
        AssertLinesConnect(built.Containers);
        var text = Sym(Text(built.Containers));

        Assert.True(built.Truncated);
        Assert.DoesNotContain("good morning everyone", text);
        Assert.Contains("what about the lexer?", text);
        Assert.Contains(
            """
            -# ││ did you profile it?
            -# ││
            -# 2 messages truncated
            -# │├ <@200> · <t>
            -# ││ yes, forty percent of runtime
            """.ReplaceLineEndings("\n"), text);
        Assert.EndsWith("<@200> · <t>\nnice, ship it", text);
    }

    /// <summary>When the quoted message alone is too long, its text is shortened as the last resort.</summary>
    [Fact]
    public async Task A_quoted_message_too_long_on_its_own_is_shortened()
    {
        var channel = new Mock<IMessageChannel>();
        var quoted = Msg(channel.Object, 1, Hime, 0, string.Concat(Enumerable.Repeat("0123456789", 50)));
        History(channel, 1);

        var built = await QuoteService.BuildQuote(quoted.Object, budget: 200);
        var text = Sym(Text(built.Containers));

        Assert.True(built.Truncated);
        Assert.Single(built.Containers);
        Assert.True(TextLength(built.Containers) <= 200);
        Assert.Matches(@"…\n-# \d+ characters truncated$", text);
    }

    [Fact]
    public async Task A_quote_that_fits_is_not_truncated()
    {
        var built = await QuoteService.BuildQuote(Quote(19), expanded: true);

        Assert.False(built.Truncated);
        Assert.DoesNotContain("truncated", Sym(Text(built.Containers)));
    }

    /// <summary>A truncated quote gets a "Show all" button in its footer, next to Jump and Expand.</summary>
    [Theory]
    [InlineData(true, false, "quote:all:20:30:0")]
    [InlineData(true, true, "quote:all:20:30:1")]
    [InlineData(false, false, null)]
    public void The_footer_offers_show_all_only_for_a_truncated_quote(bool truncated, bool expanded, string? id)
    {
        var container = new ContainerBuilder();
        var requester = new Mock<IUser>();
        requester.SetupGet(u => u.Mention).Returns("<@9>");

        QuoteService.AppendFooter(
            (container, "https://discord.com/channels/10/20/30", new JumpMessage(10, 20, 30, false), truncated),
            requester.Object, expanded);

        var buttons = container.Components.OfType<ActionRowBuilder>().Single().Components.OfType<ButtonBuilder>().ToList();
        Assert.Equal(id, buttons.SingleOrDefault(b => b.Label == "Show all")?.CustomId);
        Assert.Contains(buttons, b => b.Label == (expanded ? "Collapse" : "Expand"));
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
