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

    private static async Task<string> Render(IUserMessage quoted, bool expanded = false)
        => Sym(Text(await QuoteService.BuildMessageContainer(quoted, expanded)));

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
    public async Task Expanded_quote_nests_a_reply_to_hime_under_hime()
    {
        var text = await Render(Scenario(personBRepliesTo: 2), expanded: true);

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            -# └ <@200> · <t>
            -# · I see, if that's the case we should do that
            -# · And then we should also do this
            -# ·│
            -# ·└ <@300> · <t>
            ·· Ohh really? This is interesting!
            """.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public async Task Expanded_quote_of_a_reply_to_the_root_is_a_sibling_of_hime()
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
            -# └ <@300> · <t>
            · Ohh really? This is interesting!
            """.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public async Task Expanded_quote_of_a_reply_to_the_continuation_nests_under_hime_too()
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

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            <@200> · <t>
            -# A
            -# B
            C
            """.ReplaceLineEndings("\n"), await Render(h3.Object));

        Assert.Equal(
            """
            -# ┌ <@100> · <t>
            -# │ something
            -# │
            -# └ <@200> · <t>
            -# · A
            -# · B
            · C
            """.ReplaceLineEndings("\n"), await Render(h3.Object, expanded: true));
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
            <@200> · <t>
            -# I see, if that's the case we should do that
            -#
            -# <@200> · <t>
            And then we should also do this
            """.ReplaceLineEndings("\n"), await Render(h2.Object));
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
            [nameof(TextDisplayBuilder), nameof(MediaGalleryBuilder), nameof(TextDisplayBuilder), nameof(TextDisplayBuilder)],
            components.Select(c => c.GetType().Name));
        Assert.Equal("-# │ and this", Sym(((TextDisplayBuilder) components[2]).Content));
        Assert.StartsWith("<@300> · <t>\nnice", Sym(((TextDisplayBuilder) components[3]).Content));
    }

    [Fact]
    public async Task Image_as_the_last_thing_before_the_quoted_message_redraws_the_connector()
    {
        var channel = new Mock<IMessageChannel>();
        var a = Msg(channel.Object, 1, PersonA, 0, "something");
        var h1 = Msg(channel.Object, 2, Hime, 60, "look", replyTo: 1,
            attachments: [Image("https://cdn.discordapp.com/x/pic.png")]);
        var quoted = Msg(channel.Object, 3, PersonB, 120, "nice", replyTo: 2);
        History(channel, 3, a, h1);

        var text = await Render(quoted.Object);

        Assert.EndsWith("-# └ <@300> · <t>\n· nice", text);
    }
}
