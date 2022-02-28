using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.Core.Listeners;

public class CommandErrorHandler :
    INotificationHandler<ReactionAddedNotification>,
    INotificationHandler<ReactionRemovedNotification>
{
    private const string AssociatedErrorsKey = nameof(CommandErrorHandler) + ".AssociatedErrors";
    private const string ErrorRepliesKey = nameof(CommandErrorHandler) + ".ErrorReplies";
    private const string Emoji = "⚠";
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly IEmote _emote = new Emoji(Emoji);
    private readonly IMemoryCache _memoryCache;

    public CommandErrorHandler(DiscordSocketClient discordSocketClient, IMemoryCache memoryCache)
    {
        _discordSocketClient = discordSocketClient;
        _memoryCache         = memoryCache;
    }

    //This relates user messages with errors
    private ConcurrentDictionary<ulong, string> AssociatedErrors =>
        _memoryCache.GetOrCreate(AssociatedErrorsKey, _ => new ConcurrentDictionary<ulong, string>());

    //This relates user messages to messages containing errors
    private ConcurrentDictionary<ulong, ulong> ErrorReplies =>
        _memoryCache.GetOrCreate(ErrorRepliesKey, _ => new ConcurrentDictionary<ulong, ulong>());

    public async Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
        => await ReactionAdded(notification.Message, await notification.Channel.GetOrDownloadAsync(),
            notification.Reaction);

    public async Task Handle(ReactionRemovedNotification notification,
        CancellationToken cancellationToken)
        => await ReactionRemoved(notification.Message, await notification.Channel.GetOrDownloadAsync(),
            notification.Reaction);

    /// <summary>
    ///     Associates a user message with an error
    /// </summary>
    /// <param name="message">The message containing an errored command</param>
    /// <param name="error">The error that occurred</param>
    /// <returns></returns>
    public async Task AssociateError(IUserMessage message, string error)
    {
        if (AssociatedErrors.TryAdd(message.Id, error)) await message.AddReactionAsync(new Emoji(Emoji));
    }

    public Task AssociateError(Context context, string error) => context switch
    {
        CommandContext c => AssociateError(c.Message, error),
        _                => context.ReplyAsync(error, ephemeral: true)
    };

    private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, IMessageChannel channel,
        SocketReaction reaction)
    {
        //Don't trigger if the emoji is wrong, if the user is a bot, or if we've
        //made an error message reply already

        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

        if (reaction.Emote.Name != Emoji || ErrorReplies.ContainsKey(cachedMessage.Id)) return;

        //If the message that was reacted to has an associated error, reply in the same channel
        //with the error message then add that to the replies collection
        if (AssociatedErrors.TryGetValue(cachedMessage.Id, out var value))
        {
            var msg = await channel.SendMessageAsync("", false, new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = "https://raw.githubusercontent.com/twitter/twemoji/gh-pages/2/72x72/26a0.png",
                    Name    = "That command had an error"
                },
                Description = value,
                Footer      = new EmbedFooterBuilder { Text = "Remove your reaction to delete this message" }
            }.Build());

            if (ErrorReplies.TryAdd(cachedMessage.Id, msg.Id) == false) await msg.DeleteAsync();
        }
    }

    private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, IMessageChannel channel,
        SocketReaction reaction)
    {
        if (!reaction.User.IsSpecified || reaction.User.Value is null) return;
        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;
        if (reaction.Emote.Name != Emoji) return;

        // If there's an error reply when the reaction is removed, delete that reply,
        // remove the cached error, remove it from the cached replies, and remove
        // the reactions from the original message.
        if (ErrorReplies.TryGetValue(cachedMessage.Id, out var botReplyId) == false) return;

        await channel.DeleteMessageAsync(botReplyId);

        if
        (
            AssociatedErrors.TryRemove(cachedMessage.Id, out _) &&
            ErrorReplies.TryRemove(cachedMessage.Id, out _)
        )
        {
            var originalMessage = await cachedMessage.GetOrDownloadAsync();

            // If we know what user added the reaction, remove their and our reaction otherwise just remove ours.
            if (reaction.User.IsSpecified) await originalMessage.RemoveReactionAsync(_emote, reaction.User.Value);

            await originalMessage.RemoveReactionAsync(_emote, _discordSocketClient.CurrentUser);
        }
    }
}