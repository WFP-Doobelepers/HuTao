# The quote reply chain

This document is written in ASD-STE100 Simplified Technical English.

## 1 Purpose

This document tells you how the bot builds a quote. It gives the main rule, the
other rules, and the reason for each rule.

Read this document before you change `MessageBlock.cs`, `QuoteLayout.cs` or the
reply-chain part of `QuoteService.cs`.

## 2 Technical names and technical verbs

These words have a special meaning in this document. Each word keeps this meaning
in all parts of the document.

| Word | Meaning |
|---|---|
| Discord | The chat service that holds the messages. |
| Discord client | The Discord program that shows the messages to a user. |
| bot | The HuTao program that reads the messages and sends the quote. |
| user | The person who sends a message or reads a quote. |
| message | One message in a Discord channel. |
| author | The user or the webhook that sends a message. |
| webhook | A Discord connection that sends messages. All its messages have the same author identification, but each message can have a different display name. |
| display name | The name that Discord shows above a message. |
| usual message | A message with the Discord type Default. A reply, a system message and a command message are not usual messages. |
| system message | A message that Discord sends about the channel, for example a pin message or a member-join message. |
| command message | A message that Discord sends as the result of an application command or a context menu command. |
| reply | A message that points to an earlier message. Discord shows the earlier message above it. |
| reply reference | The pointer that Discord puts on a reply. It gives the identification of the earlier message. |
| forwarded message | A message that a user copies from a different channel. Discord puts a reference on it, but it is not a reply. |
| thread | A Discord thread that starts at a message. |
| quote | The text block that the bot sends when a user sends a message link. |
| conversation | The messages that one reply chain connects. |
| turn | All the messages that one author sends when no other author sends a message. See paragraph 4.1. |
| header | The line that shows the author and the time above a group of messages. |
| header group | The messages below one header. See paragraph 4.2. |
| reply chain | The turns above the quoted turn. See paragraph 4.3. |
| root | The first turn of the reply chain. It is the oldest turn in the quote. |
| trunk | The reply chain and the quoted turn. |
| window | The messages that the bot reads before the quoted message. See paragraph 5.1. |
| collapsed mode | The first quote. The bot shows only the trunk. |
| expanded mode | The quote after a user pushes the Expand button. The bot shows all the turns in the window that the root connects, and the loose turns between them. |
| loose turn | A turn that no reply connects to the conversation, but that a user sent after the root and before the quoted message. |
| tree | The arrangement of the turns in expanded mode. A turn is below the turn that it replies to. |
| parent | The turn that contains the message at the reply reference. |
| child | A turn whose first message replies to a message in a different turn. |
| sibling | A turn that has the same parent as a different turn. |
| fork | A turn that has two or more children. |
| connector | The symbol at the start of a line that shows the shape of the reply chain. |
| rail | A vertical line of connectors. A rail connects the turns that are on it. |
| main rail | The rail at the left side of the quote. The trunk is on it. |
| content column | The part of a line to the right of the connector. The bot puts the message text there. |
| class | A named part of the code. |
| function | A named part of the code that does one operation. |
| permissions | The Discord settings that say which channels a user or the bot can read. |
| log file | The file where the bot writes messages about its operation. |

## 3 The main rule

A quote shows the conversation that the reply chain connects. The quote shows
this conversation in the same shape as the Discord client.

Two rules come from this main rule:

- Rule 1: Show a message only if the reply chain connects it. Expanded mode also
  shows the loose turns, without a connector.
- Rule 2: Show the messages in the same shape as the Discord client.

Rule 1 removes the messages of other conversations from collapsed mode. Two users
can speak in one channel at the same time. Expanded mode shows all the messages
of that time, but only the reply chain connects them.

Rule 2 makes the quote easy to read. The user knows this shape, because the
Discord client uses it.

The two rules are not connected. Rule 1 finds the messages. Rule 2 puts them in
sequence. A change to one rule must not change the other rule.

## 4 Definitions

### 4.1 Turn

A turn is a set of messages from one author. No other author sends a message
between them.

A turn starts at a message. The first message of a turn is a usual message, a
reply or a command message. A turn cannot start at a system message.

The turn continues with the next message if all of these conditions are true:

- The next message is a usual message.
- The next message has no thread.
- The next message has the same author.
- If the author is a webhook, the next message has the same display name.

Note: Time is not a condition. A long time between two messages does not stop a
turn.

The code for a turn is the `MessageBlock` class. The code for these conditions is
the `MessageBlock.IsContinuation` function.

### 4.2 Header group

A header group is a part of a turn. The bot shows one header above each header
group.

A new header group starts when the time between two messages is 7 minutes or
more. The bot measures this time from the first message of the header group. The
bot does not measure this time from the previous message.

A turn with no long time between its messages has one header group. A turn with a
long time between two messages has two header groups or more.

The code for the header groups is the `MessageBlock.Flattened` function.

### 4.3 Reply chain

The reply chain is a list of turns above the quoted turn. The bot makes this list
with these steps:

1. The bot finds the turn that contains the quoted message.
2. The bot reads the reply reference of the first message of this turn.
3. The bot finds the turn that contains the message at this reply reference.
4. The bot adds this turn to the list.
5. The bot does step 2 to step 4 again for the new turn.

The bot stops when one of these conditions is true:

- The first message of the turn is not a reply.
- The bot cannot find the message at the reply reference.
- The reply reference points to the quoted turn, or to a turn that is already in
  the list.
- The list has 10 turns.

Note: The bot reads the reply reference of the first message of the turn. It does
not read the reply reference of the quoted message. A user can quote a message
that is not a reply. The turn of that message can still have a parent.

In step 3, the bot first looks in the window. If the message is not in the window,
the bot uses the copy that Discord sends with the reply. If there is no copy, the
bot asks Discord for the message.

The code for the reply chain is the `MessageBlock.WalkChainAsync` function.

## 5 How the bot builds a quote

### 5.1 Step 1: Read the window

A user sends a message that contains a message link. The bot reads the last 100
messages before the message at that link. These messages are the window.

Note: Only a message from a user starts a quote. A message from a bot or from a
webhook does not start a quote.

If the bot cannot read the earlier messages of the channel, the window is empty.
The bot writes a message to the log file and continues.

### 5.2 Step 2: Divide the window into turns

The bot divides the window and the quoted message into turns. The bot uses the
conditions in paragraph 4.1.

### 5.3 Step 3: Find the reply chain

The bot makes the reply chain. The bot uses the steps in paragraph 4.3.

If the reply chain is empty, the bot shows the quoted message alone. The bot does
not show the window. This is also the result when the bot cannot get the parent
message.

### 5.4 Step 4: Link the turns

The two modes are different only in this step.

- In collapsed mode, the bot links only the trunk. Each turn of the reply chain
  has one child. This child is the next turn of the trunk.
- In expanded mode, the bot links all the turns in the window. A turn is a child
  of the turn that contains its reply reference. When a turn has more than one
  child, the child in the trunk is last.

In expanded mode, the bot also collects the loose turns. It does not collect a
loose turn from a bot, because the earlier quotes of the bot are posts from a
bot. It collects a loose turn from a webhook, because persons speak through
webhooks. In collapsed mode, a turn that the root does not connect is not in the
quote.

The code for this step is the `MessageBlock.LinkChain` function and the
`MessageBlock.LinkChildren` function.

### 5.5 Step 5: Put the turns on rails

The two modes use the same layout. The layout uses these rules:

1. The trunk is on the main rail.
2. Siblings are on the same rail.
3. If a turn is not in the trunk and has a sibling on its rail, the children of
   that turn go on a new rail. The new rail is one column to the right, below the
   text of that turn.
4. If a turn has no sibling on its rail, its child continues on the same rail. A
   turn in the trunk always continues on the main rail.
5. The quoted turn is the last turn on the main rail. It has no connector. The
   main rail goes directly into its header.
6. A loose turn goes directly after the turn that a user sent before it. It is in
   the content column of that turn, with its own header and no connector.

Each turn, except the quoted turn, has one of these connectors at its header:

- `reply_right` (`┌`): the first turn of the quote.
- `reply_t` (`├`): a turn that has more turns below it on its rail.
- `reply` (`└`): the last turn of a rail that is not the main rail.

A `reply_line` (`│`) continues a rail. A `reply_spacer` (`·`) fills a column where
a rail stopped.

In the two modes, the bot shows each header group with its own header. The extra
headers are in the content column. They do not have a connector, thus they do not
look like a new turn.

An image stops the text. The rails stop above the image and continue below it.

The code for this step is the `QuoteLayout` class.

### 5.6 Step 6: Fit the quote into one message

The quote is always one Discord message, in collapsed mode and in expanded mode.
The text of the quote can have a maximum of 3800 characters. If the quote is too
long, the bot does these steps in sequence. After each step, the bot measures the
quote again. The bot stops when the quote fits.

1. The bot removes the loose turns, one at a time. The oldest loose turn goes
   first.
2. The bot hides turns in the middle of the quote. It hides one turn, then two
   turns, and continues. The hidden turns are always in the middle. The first
   turn and the quoted turn stay.
3. The bot hides the first turn too. Only the quoted turn stays.
4. The bot shortens the text of the quoted message, and adds an ellipsis.

Where the bot removes something, it puts one line at the same place. The line
tells what is not shown, for example `truncated (12 messages)`.

- Where the bot hides turns in the middle, the line has a separator above it and
  a separator below it.
- Where the bot removes only loose turns, the line is in the content column where
  the loose turns were. It has no separators, thus the rails stay connected. A
  separator is a component, and a message can have a maximum of 40 components.
- Where the bot shortens the quoted message, the line is directly below the
  shortened text, for example `truncated (1,234 characters)`.

When the bot removes something, the quote also gets a Show all button, next to
the Jump button. When a user pushes it, the bot sends the full quote to that user
only. The full quote has no limit, thus it can be more than
one message. Each message is a different Discord message, thus a rail cannot go
from one message to the next. The bot ends a message where a turn starts, if
possible. The bot sends the full quote only to a user who can read the channel of
the quoted message.

For each message, the bot also shows the images. For the quoted message only, the
bot also shows the other files, the text of the embeds, and the text of the
components.

The quote has a Jump button and an Expand button. When a user pushes the Expand
button, the bot does step 1 to step 6 again in expanded mode. It then changes the
same quote. The button then becomes a Collapse button. Because of step 6, the
changed quote always fits in the same message.

If the bot cannot find the message at the link, and the message is in the deleted
message log, the bot builds the quote from the log. This path uses different code
and does not use the turns.

## 6 The reasons for the rules

### 6.1 Why a turn, and not a message

The Discord client shows one header for one turn, if the turn has no long time
between two messages. If the bot shows one header for each message, the user sees
the same author many times.

This is the defect in the first report. The bot showed this text:

```
hime-san:
A

hime-san:
B

hime-san:
C
```

The correct text is:

```
hime-san:
A
B
C
```

### 6.2 Why a long time between two messages changes only the header

The Discord client uses a limit of 420000 milliseconds, which is 7 minutes. The
client function is `shouldStartNewGroup`.

The client shows a new header after a long time between two messages. The client
does not remove the message from the channel. The user still sees the message.

Thus the bot must also show the message. The bot only adds a header.

Caution: An earlier version of the bot removed the message from the turn after a
long time between two messages. The bot then did not show the message in the
quote. This was incorrect, because the user sees that message in Discord.

### 6.3 Why the bot measures the time from the first message of the header group

The Discord client measures the time from the first message of the header group.

An author can send a message every 6 minutes for one hour. The time between two
messages is always less than 7 minutes. If the bot measures from the previous
message, the bot makes one header group for the full hour. The Discord client
makes six header groups.

### 6.4 Why a reply always starts a new turn

A reply points to a different message. It is a different action. The Discord
client also shows a new header at each reply.

### 6.5 Why a message from a different author is not connected

A message from a different author is not part of the turn. No reply points to it.
Thus the reply chain does not connect it. Collapsed mode does not show it.

This is the second defect in the first report. A message such as `Good morning!`
came into the quote, because the earlier code put each message below the nearest
author above it.

Expanded mode shows all the messages of the conversation time, thus it shows
`Good morning!` as a loose turn. The loose turn has no connector, because no
reply connects it. It has its own header, as a message after a long time does
(paragraph 6.2). A webhook post shows the display name of the webhook in bold,
because Discord cannot show a mention of a webhook.

### 6.6 Why a straight reply chain stays on one rail

An earlier version of the bot moved each reply one column to the right. A long
reply chain then went far to the right, and the text became narrow. A straight
reply chain has no fork, thus a new column does not give the user more
information.

The bot adds a column only below a turn that has a sibling after it. The new
column keeps the children of that turn apart from the sibling. Without the new
column, the children look like replies to the sibling.

This rule has a cost. Two different conversations can give the same text:

- Turn B replies to turn A, and turn C replies to turn B.
- Turn B and turn C both reply to turn A.

In the two conversations, the quote shows turn A, turn B and turn C on one rail.
This cost is accepted, because a quote with fewer columns is easier to read.

### 6.7 Why the quoted turn has no connector

The Discord client shows a reply below the message that it replies to. A line
goes from the reply up to that message. The reply itself has no connector.

The quoted turn is the reply that the user asks for. Thus the bot shows it in the
same shape: the main rail goes directly into its header.

### 6.8 Why the two modes use one layout

An earlier version of the bot had a different layout for each mode. The same
turns then had different connectors in the two modes.

Now the two modes are different only in the turns that they link (step 4). The
layout (step 5) is the same. Thus collapsed mode shows the same text as expanded
mode, without the turns that are not in the trunk.

### 6.9 Why a quote is always one message

The Expand button changes one message. A message cannot become two messages.
Thus an expanded quote that needs two messages cannot show in full. An earlier
version of the bot then showed only the last part.

The steps in paragraph 5.6 remove the least important text first. A loose turn
is not part of the conversation, thus it goes first. The middle of the quote goes
next, because the first turn tells how the conversation starts, and the turns
near the quoted message tell what it answers. The quoted message goes last,
because it is the reason for the quote.

The Show all button gives the full quote to the user who asks for it. Other users
in the channel do not see the long text.

### 6.10 Why the window is the 100 messages before the quoted message

The bot must know if the quoted message is part of a longer turn. The quoted
message can be a message that is not a reply. The parent then comes from the first
message of its turn.

The bot cannot find the turn without the messages before the quoted message. Thus
the bot reads them first.

### 6.11 Why a forwarded message and a system message are not replies

Discord puts a reference on a forwarded message, on a pin message and on a
thread-start message. These references are not reply references.

The bot accepts a reference only from a message with the Discord type Reply. If
the bot accepts the other references, the bot shows a reply chain that does not
exist.

### 6.12 Why the bot must compare the webhook name

All messages from one webhook have the same author identification. A webhook can
send each message with a different display name.

Two different persons can speak through one webhook. Thus a new display name is a
new author, and the bot starts a new turn. The messages after the change are then
in a different turn, and the quote shows them only if the reply chain connects
them.

If the bot does not compare the display name, the bot shows the words of one
person below the name of a different person.

## 7 Examples

In the text below, these symbols replace the Discord emoji: `┌` is `reply_right`,
`├` is `reply_t`, `└` is `reply`, `│` is `reply_line`, and `·` is `reply_spacer`.
The bot shows a mention and a relative time in each header.

### 7.1 A straight reply chain

The channel has these messages:

| Number | Author | Message | Reply to |
|---|---|---|---|
| 1 | person A (100) | something | - |
| 2 | hime-san (200) | I see | 1 |
| 3 | hime-san (200) | And then | - |
| 4 | person B (300) | Good morning! | - |
| 5 | person B (300) | Ohh really? | 2 |

The turns are:

- Turn 1: message 1.
- Turn 2: message 2 and message 3.
- Turn 3: message 4.
- Turn 4: message 5.

A user quotes message 5. The reply chain is turn 2 and turn 1.

The quote in collapsed mode is:

```
-# ┌ <@100> · <t:1756468800:R>
-# │ something
-# │
-# ├ <@200> · <t:1756468860:R>
-# │ I see
-# │ And then
-# │
<@300> · <t:1756469010:R>
Ohh really?
```

Message 4 is not in the quote. The root does not connect turn 3.

The quote in expanded mode shows message 4 as a loose turn, after turn 2:

```
-# ┌ <@100> · <t:1756468800:R>
-# │ something
-# │
-# ├ <@200> · <t:1756468860:R>
-# │ I see
-# │ And then
-# │
-# │ <@300> · <t:1756468920:R>
-# │ Good morning!
-# │
<@300> · <t:1756469010:R>
Ohh really?
```

If message 3 comes 7 minutes after message 2, the quote is:

```
-# ┌ <@100> · <t:1756468800:R>
-# │ something
-# │
-# ├ <@200> · <t:1756468860:R>
-# │ I see
-# │
-# │ <@200> · <t:1756469280:R>
-# │ And then
-# │
<@300> · <t:1756469340:R>
Ohh really?
```

Message 3 stays in the quote. It has a second header and a second time.

### 7.2 A fork

The channel has these messages:

| Number | Author | Message | Reply to |
|---|---|---|---|
| 1 | person A (100) | something | - |
| 2 | hime-san (200) | I see | 1 |
| 3 | person B (300) | Good point | 1 |
| 4 | person C (400) | Agreed | 3 |
| 5 | person D (500) | Ohh really? | 1 |

A user quotes message 5. Turn 1 is a fork: message 2, message 3 and message 5
reply to it. The trunk is turn 1 and turn 5.

The quote in expanded mode is:

```
-# ┌ <@100> · <t:1756468800:R>
-# │ something
-# │
-# ├ <@200> · <t:1756468860:R>
-# │ I see
-# │
-# ├ <@300> · <t:1756468920:R>
-# │ Good point
-# ││
-# │└ <@400> · <t:1756468950:R>
-# │· Agreed
-# │
<@500> · <t:1756468980:R>
Ohh really?
```

Message 2, message 3 and message 5 are siblings, thus they are on the main rail.
Message 3 has a sibling after it, thus its child (message 4) goes on a new rail.

The quote in collapsed mode is:

```
-# ┌ <@100> · <t:1756468800:R>
-# │ something
-# │
<@500> · <t:1756468980:R>
Ohh really?
```

This is the text of expanded mode, without the turns that are not in the trunk.

## 8 Limits

These limits are known. Change them only with a test.

- The window is 100 messages. If a turn starts before the window, the bot shows
  only the part of that turn that is in the window. The bot also does not get the
  reply reference of the first message of that turn.
- The window has only messages before the quoted message. Thus the quote does not
  show a reply that a user sends after the quoted message. This is also true in
  expanded mode.
- If the bot gets a parent message from outside the window, that turn has one
  message only. The bot does not show the other messages of that turn.
- The reply chain has a maximum of 10 turns above the quoted turn. Thus the quote
  shows a maximum of 11 turns.
- For each message before the quoted message, the bot shows a maximum of 200
  characters and then an ellipsis. This limit applies to the message content, to
  the text of the embeds, and to the text of the components.
- The Discord client wraps a long line of text. The wrapped part has no
  connector, thus the rail has a gap there. The 200-character limit keeps most
  lines short.
- The bot does not use the calendar-day rule of the Discord client, because the
  bot does not know the time zone of the user.
- The bot reads the window with the permissions of the bot. It does not read the
  window with the permissions of the user who sends the message link.

## 9 Tests

These test files test the rules of this document:

- `HuTao.Tests/Services/Quote/MessageBlockTests.cs` — the turns, the header
  groups, and the reply chain.
- `HuTao.Tests/Services/Quote/QuoteRenderingTests.cs` — the text of the quote.
  Most of these tests compare the full text, character by character. Each test
  without an image also makes sure that each connector touches a different
  connector, or the text directly above it, or the header of the quoted turn.

Add a test to these files when you change a rule in this document.
