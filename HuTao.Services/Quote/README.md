# The quote reply chain

This document is written in ASD-STE100 Simplified Technical English.

## 1 Purpose

This document tells you how the bot builds a quote. It gives the principle, the
rules, and the reason for each rule.

Read this document before you change `MessageBlock.cs` or the reply-chain part of
`QuoteService.cs`.

## 2 Technical names and technical verbs

These words have a special meaning in this document. Each word keeps this meaning
in all parts of the document.

| Word | Meaning |
|---|---|
| message | One message in a Discord channel. |
| author | The user or the webhook that sends a message. |
| reply | A message that points to an earlier message. Discord shows the earlier message above it. |
| quote | The block of text that the bot sends when a user sends a message link. |
| turn | All the messages that one author sends without an interruption. See paragraph 4.1. |
| header | The line that shows the author and the time above a group of messages. |
| header group | The messages below one header. See paragraph 4.2. |
| reply chain | The turns from the quoted turn up to the first turn. See paragraph 4.3. |
| window | The messages that the bot reads before the quoted message. See paragraph 5.1. |
| collapsed mode | The default quote. The bot shows the chain as a straight line. |
| expanded mode | The quote after a user pushes the Expand button. The bot shows a tree. |

## 3 The principle

A quote shows the conversation that the reply chain connects. The quote shows
this conversation in the same shape as the Discord client.

Two rules come from this principle:

- Rule 1: Show a message only if the reply chain connects it.
- Rule 2: Show the messages in the same shape as the Discord client.

Rule 1 keeps other conversations out of the quote. Two persons can speak in the
same channel at the same time. Rule 1 removes the messages of the other
conversation.

Rule 2 makes the quote easy to read. The reader knows this shape, because the
Discord client uses it.

The two rules are independent. Rule 1 selects the messages. Rule 2 arranges them.
A change to one rule must not change the other rule.

## 4 Definitions

### 4.1 Turn

A turn is a set of messages from one author. No other author sends a message
between them.

A turn starts at a message. The first message of a turn is one of these types:

- a usual message
- a reply
- an application command
- a context menu command

A system message is not one of these types. A pin message and a member-join
message are system messages. A turn cannot continue after a system message.

The turn continues with the next message if all of these conditions are true:

- The next message is a usual message. A reply and a system message are not usual
  messages.
- The next message has no thread.
- The next message has the same author.
- The author is a webhook, and the display name is the same. This condition
  applies only to a webhook.

Note: Time is not a condition. A long pause does not stop a turn.

The code for a turn is the `MessageBlock` class. The code for the conditions is
the `MessageBlock.IsContinuation` function.

### 4.2 Header group

A header group is a part of a turn. The bot shows one header above each header
group.

A new header group starts when the time between two messages is 7 minutes or
more. The bot measures this time from the first message of the header group. The
bot does not measure this time from the previous message.

A turn with a short pause has one header group. A turn with a long pause has two
header groups or more.

The code for the header groups is the `MessageBlock.Flattened` function.

### 4.3 Reply chain

The reply chain is a list of turns. The bot makes this list with these steps:

1. The bot finds the turn that contains the quoted message.
2. The bot reads the reply reference of the first message of this turn.
3. The bot finds the turn that contains the message at this reference.
4. The bot adds this turn to the list.
5. The bot does step 2 to step 4 again for the new turn.

The bot stops when one of these conditions is true:

- The first message of the turn is not a reply.
- The bot cannot find the message at the reference.
- The list has 10 turns.

Note: The bot reads the reply reference of the first message of the turn. It does
not read the reply reference of the quoted message. A user can quote a message
that is not a reply. The turn of that message can still have a parent.

The code for the reply chain is the `MessageBlock.WalkChainAsync` function.

## 5 How the bot builds a quote

### 5.1 Step 1: Read the window

The bot reads the last 100 messages before the quoted message. These messages are
the window.

If the bot cannot read the channel history, the window is empty. The bot writes a
warning to the log and continues.

### 5.2 Step 2: Divide the window into turns

The bot divides the window and the quoted message into turns. The bot uses the
conditions in paragraph 4.1.

### 5.3 Step 3: Find the reply chain

The bot makes the reply chain. The bot uses the steps in paragraph 4.3.

If the reply chain is empty, the bot shows the quoted message alone. The bot does
not show the window.

### 5.4 Step 4: Show the turns

In collapsed mode, the bot shows the turns of the reply chain in a straight line.
The oldest turn is first. The quoted turn is last.

In expanded mode, the bot makes a tree. A turn is a child of the turn that
contains its reply reference. The bot shows all the turns in this tree.

In both modes, the bot shows each header group with its own header. The extra
headers are in the content column. They have no connector symbol.

## 6 The reasons for the rules

### 6.1 Why a turn, and not a message

The Discord client shows one header for one turn. If the bot shows one header for
each message, the reader sees the same author many times.

This is the fault in the first report. The bot showed this text:

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

### 6.2 Why a long pause changes only the header

The Discord client uses a limit of 420000 milliseconds, which is 7 minutes. The
client function is `shouldStartNewGroup`.

The client draws a new header after a long pause. The client does not remove the
message from the channel. The reader still sees the message.

Therefore the bot must also show the message. The bot only adds a header.

Caution: An earlier version of the bot removed the message from the turn after a
long pause. The message then disappeared from the quote. This was incorrect,
because the reader sees that message in Discord.

### 6.3 Why the bot measures the pause from the first message of the header group

The Discord client measures the time from the first message of the header group.

An author can send a message every 6 minutes for one hour. Each interval is less
than 7 minutes. If the bot measures from the previous message, the bot makes one
header group for the full hour. The Discord client makes ten header groups.

### 6.4 Why a reply always starts a new turn

A reply points to a different message. It is a different action. The Discord
client also starts a new header at each reply.

### 6.5 Why a message from a different author is not in the quote

A message from a different author is not part of the turn. No reply points to it.
Therefore the reply chain does not connect it, and Rule 1 removes it.

This is the second fault in the first report. A message such as `Good morning!`
came into the quote, because the earlier code attached each message to the
nearest author above it.

### 6.6 Why the quoted turn stays below its parent in the tree

In expanded mode, the bot moves a turn up to the level of its parent while the
chain has one branch only. This keeps a long chain narrow.

The bot does not move the quoted turn up. The position of the quoted turn shows
the reader which message the author answered.

If the bot moves the quoted turn up, two different conversations look the same. A
reply to the first author and a reply to the second author give the same text.

### 6.7 Why the window is the 100 messages before the quoted message

The bot must know if the quoted message is part of a longer turn. The quoted
message can be a message that is not a reply. Then the parent comes from the
first message of its turn.

The bot cannot find the turn without the messages before the quoted message.
Therefore the bot reads them first.

### 6.8 Why forwards and system messages are not replies

Discord adds a reference to a forward, to a pin message, and to a thread start
message. These references are not replies.

The bot accepts a reference only from a message with the type `Reply`. If the bot
accepts the other references, the bot shows a reply chain that does not exist.

### 6.9 Why a webhook needs a name comparison

All messages from one webhook have the same author identifier. A webhook can send
each message with a different display name.

The Discord client starts a new header when the display name changes. The bot
does the same. If the bot does not compare the name, the bot shows the words of
one person below the name of a different person.

## 7 Example

The channel has these messages:

| Number | Author | Message | Reply to |
|---|---|---|---|
| 1 | person A | something | - |
| 2 | hime-san | I see, if that is the case we should do that | 1 |
| 3 | hime-san | And then we should also do this | - |
| 4 | person B | Good morning! | - |
| 5 | person B | Ohh really? This is interesting! | 2 |

The turns are:

- Turn 1: message 1.
- Turn 2: message 2 and message 3.
- Turn 3: message 4.
- Turn 4: message 5.

A user quotes message 5. The reply chain is turn 4, turn 2, turn 1.

The quote in collapsed mode is:

```
person A
  something

hime-san
  I see, if that is the case we should do that
  And then we should also do this

person B
  Ohh really? This is interesting!
```

Message 4 is not in the quote. Turn 3 is not in the reply chain.

If message 3 comes 7 minutes after message 2, the quote is:

```
person A
  something

hime-san
  I see, if that is the case we should do that

hime-san
  And then we should also do this

person B
  Ohh really? This is interesting!
```

Message 3 stays in the quote. It has a second header and a second time.

## 8 Limits

These limits are known. Change them only with a test.

- The window is 100 messages. A turn that starts before the window loses the
  messages that come after its first message.
- The reply chain has a maximum of 10 turns.
- The bot shows a maximum of 200 characters for each message before the quoted
  message.
- The bot does not use the calendar-day rule of the Discord client. The bot has
  no time zone for the reader.
- The bot reads the window with the permissions of the bot. It does not read the
  window with the permissions of the user who sends the message link.

## 9 Tests

These test files hold the rules of this document:

- `HuTao.Tests/Services/Quote/MessageBlockTests.cs` — the turns, the header
  groups, and the reply chain.
- `HuTao.Tests/Services/Quote/QuoteRenderingTests.cs` — the text of the quote.
  These tests compare the full text, character by character.

Add a test to these files when you change a rule in this document.
