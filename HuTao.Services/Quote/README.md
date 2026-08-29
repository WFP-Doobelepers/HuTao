# The quote reply chain

This document is written in ASD-STE100 Simplified Technical English.

## 1 Purpose

This document tells you how the bot builds a quote. It gives the main rule, the
other rules, and the reason for each rule.

Read this document before you change `MessageBlock.cs` or the reply-chain part of
`QuoteService.cs`.

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
| window | The messages that the bot reads before the quoted message. See paragraph 5.1. |
| collapsed mode | The first quote. The bot shows the reply chain as a straight line. |
| expanded mode | The quote after a user pushes the Expand button. The bot shows a tree. |
| tree | The arrangement of the turns in expanded mode. A turn is below the turn that it replies to. |
| parent | The turn that contains the message at the reply reference. |
| child | A turn whose first message replies to a message in a different turn. |
| connector | The symbol at the start of a line that shows the shape of the reply chain. |
| content column | The part of a line to the right of the connector. The bot puts the message text there. |
| class | A named part of the code. |
| function | A named part of the code that does one operation. |
| permissions | The Discord settings that say which channels a user or the bot can read. |
| log file | The file where the bot writes messages about its operation. |

## 3 The main rule

A quote shows the conversation that the reply chain connects. The quote shows
this conversation in the same shape as the Discord client.

Two rules come from this main rule:

- Rule 1: Show a message only if the reply chain connects it.
- Rule 2: Show the messages in the same shape as the Discord client.

Rule 1 removes the messages of other conversations from the quote. Two users can
speak in one channel at the same time.

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

### 5.4 Step 4: Show the turns

In collapsed mode, the bot shows the turns of the reply chain in a straight line.
The oldest turn is first. The quoted turn is last.

In expanded mode, the bot shows a tree. A turn is a child of the turn that
contains its reply reference. The bot shows all the turns of the tree. When a turn
has more than one child, the bot puts the child that is in the reply chain last.

In the two modes, the bot shows each header group with its own header. The extra
headers are in the content column. They do not have a connector, thus they do not
look like a new turn in the reply chain.

### 5.5 Step 5: Send the quote

The bot puts the text in a container. A container holds a maximum of 3800
characters of text. If the text is longer, the bot sends more than one container.

For each message, the bot also shows the images. For the quoted message only, the
bot also shows the other files, the text of the embeds, and the text of the
components.

The quote has a Jump button and an Expand button. When a user pushes the Expand
button, the bot does step 1 to step 5 again in expanded mode. It then changes the
same quote. The button then becomes a Collapse button.

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

### 6.5 Why a message from a different author is not in the quote

A message from a different author is not part of the turn. No reply points to it.
Thus the reply chain does not connect it, and Rule 1 removes it.

This is the second defect in the first report. A message such as `Good morning!`
came into the quote, because the earlier code put each message below the nearest
author above it.

### 6.6 Why the quoted turn stays below its parent in the tree

In expanded mode, the bot moves a turn up to the level of its parent if that
parent has only one child, and that child is in the reply chain. This prevents too
many levels in a long reply chain.

The bot does not move the quoted turn up. The position of the quoted turn shows
the user which message the author answered.

If the bot moves the quoted turn up, two different conversations look the same. A
reply to the first author and a reply to the second author then give the same
text.

### 6.7 Why the window is the 100 messages before the quoted message

The bot must know if the quoted message is part of a longer turn. The quoted
message can be a message that is not a reply. The parent then comes from the first
message of its turn.

The bot cannot find the turn without the messages before the quoted message. Thus
the bot reads them first.

### 6.8 Why a forwarded message and a system message are not replies

Discord puts a reference on a forwarded message, on a pin message and on a
thread-start message. These references are not reply references.

The bot accepts a reference only from a message with the Discord type Reply. If
the bot accepts the other references, the bot shows a reply chain that does not
exist.

### 6.9 Why the bot must compare the webhook name

All messages from one webhook have the same author identification. A webhook can
send each message with a different display name.

Two different persons can speak through one webhook. Thus a new display name is a
new author, and the bot starts a new turn. The messages after the change are then
in a different turn, and the quote shows them only if the reply chain connects
them.

If the bot does not compare the display name, the bot shows the words of one
person below the name of a different person.

## 7 Example

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

In the text below, these symbols replace the Discord emoji: `┌` is `reply_right`,
`├` is `reply_t`, `└` is `reply`, `│` is `reply_line`, and `·` is `reply_spacer`.
The bot shows a mention and a relative time in each header.

The quote in collapsed mode is:

```
-# ┌ <@100> · <t:1756468800:R>
-# │ something
-# │
-# ├ <@200> · <t:1756468860:R>
-# │ I see
-# │ And then
<@300> · <t:1756469010:R>
Ohh really?
```

Message 4 is not in the quote. Turn 3 is not in the reply chain.

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
<@300> · <t:1756469340:R>
Ohh really?
```

Message 3 stays in the quote. It has a second header and a second time.

## 8 Limits

These limits are known. Change them only with a test.

- The window is 100 messages. If a turn starts before the window, the bot shows
  only the part of that turn that is in the window. The bot also does not get the
  reply reference of the first message of that turn.
- If the bot gets a parent message from outside the window, that turn has one
  message only. The bot does not show the other messages of that turn.
- The reply chain has a maximum of 10 turns above the quoted turn. Thus the quote
  shows a maximum of 11 turns.
- For each message before the quoted message, the bot shows a maximum of 200
  characters and then an ellipsis. This limit applies to the message content, to
  the text of the embeds, and to the text of the components.
- The bot does not use the calendar-day rule of the Discord client, because the
  bot does not know the time zone of the user.
- The bot reads the window with the permissions of the bot. It does not read the
  window with the permissions of the user who sends the message link.

## 9 Tests

These test files test the rules of this document:

- `HuTao.Tests/Services/Quote/MessageBlockTests.cs` — the turns, the header
  groups, and the reply chain.
- `HuTao.Tests/Services/Quote/QuoteRenderingTests.cs` — the text of the quote.
  Most of these tests compare the full text, character by character.

Add a test to these files when you change a rule in this document.
