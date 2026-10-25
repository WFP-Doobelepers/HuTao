using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Services.Core.Messages;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Core.Listeners;

public class CommandErrorHandler(DiscordSocketClient discordSocketClient, IMemoryCache memoryCache)
    : INotificationHandler<ReactionAddedNotification>,
      INotificationHandler<ReactionRemovedNotification>
{
    /// <summary>How long an error stays available behind its reaction.</summary>
    public static readonly TimeSpan ErrorLifetime = TimeSpan.FromHours(1);

    private const string AssociatedErrorsKey = nameof(CommandErrorHandler) + ".AssociatedErrors";
    private const string ErrorRepliesKey = nameof(CommandErrorHandler) + ".ErrorReplies";
    private const string Emoji = "⚠";
    private readonly IEmote _emote = new Emoji(Emoji);
    private readonly object _gate = new();

    // Each entry expires by itself, so nothing piles up for messages that nobody reacts to.
    private string AssociatedKey(ulong messageId) => $"{AssociatedErrorsKey}.{messageId}";

    private string ReplyKey(ulong messageId) => $"{ErrorRepliesKey}.{messageId}";

    internal bool TryGetError(ulong messageId, out string error)
        => memoryCache.TryGetValue(AssociatedKey(messageId), out error!);

    private bool TryGetReply(ulong messageId, out ulong replyId)
        => memoryCache.TryGetValue(ReplyKey(messageId), out replyId);

    private bool TryAddError(ulong messageId, string error)
    {
        lock (_gate)
        {
            if (memoryCache.TryGetValue(AssociatedKey(messageId), out _)) return false;
            memoryCache.Set(AssociatedKey(messageId), error, ErrorLifetime);
            return true;
        }
    }

    private bool TryAddReply(ulong messageId, ulong replyId)
    {
        lock (_gate)
        {
            if (memoryCache.TryGetValue(ReplyKey(messageId), out _)) return false;
            memoryCache.Set(ReplyKey(messageId), replyId, ErrorLifetime);
            return true;
        }
    }

    public Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
        => notification.Reaction.User is { IsSpecified: true, Value.IsBot: true }
            ? Task.CompletedTask
            : ShowErrorAsync(notification.Message.Id, notification.Message.GetOrDownloadAsync,
                notification.Channel.GetOrDownloadAsync, notification.Reaction.UserId, notification.Reaction.Emote.Name);

    public async Task Handle(
        ReactionRemovedNotification notification,
        CancellationToken cancellationToken)
        => await ReactionRemoved(notification.Message, await notification.Channel.GetOrDownloadAsync(),
            notification.Reaction);

    public async Task AssociateError(IUserMessage message, string error)
    {
        if (TryAddError(message.Id, error)) await message.AddReactionAsync(new Emoji(Emoji));
    }

    public Task AssociateError(Context context, string error) => context switch
    {
        CommandContext c => AssociateError(c.Message, error),
        _                => context.ReplyAsync(error, ephemeral: true)
    };

    // Shows the stored error to the member who ran the command. Any member can put the warning sign on any message, so
    // the stored error is checked first, and only a message that has one is downloaded.
    internal async Task ShowErrorAsync(
        ulong messageId, Func<Task<IUserMessage>> getMessage, Func<Task<IMessageChannel>> getChannel,
        ulong userId, string emoteName)
    {
        if (emoteName != Emoji || TryGetReply(messageId, out _)) return;
        if (!TryGetError(messageId, out var value)) return;

        // A message deleted since the command ran has nothing to answer.
        if (await getMessage() is not { } message || message.Author.Id != userId) return;

        const uint accentColor = 0x9B59FF;
        const int maxErrorChars = 3200;

        var container = new ContainerBuilder()
            .WithSection(new SectionBuilder()
                .WithTextDisplay($"## That command had an error\n{value.Truncate(maxErrorChars)}")
                .WithAccessory(new ThumbnailBuilder(
                    new UnfurledMediaItemProperties("https://raw.githubusercontent.com/twitter/twemoji/gh-pages/2/72x72/26a0.png"))))
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay("-# Remove your reaction to delete this message")
            .WithAccentColor(accentColor);

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .Build();

        var channel = await getChannel();
        var msg = await channel.SendMessageAsync(
            components: components,
            allowedMentions: AllowedMentions.None);

        if (TryAddReply(messageId, msg.Id) == false) await msg.DeleteAsync();
    }

    private async Task ReactionRemoved(
        Cacheable<IUserMessage, ulong> cachedMessage, IMessageChannel channel,
        SocketReaction reaction)
    {
        if (!reaction.User.IsSpecified || reaction.User.Value is null) return;
        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;
        if (reaction.Emote.Name != Emoji) return;

        if (TryGetReply(cachedMessage.Id, out var botReplyId) == false) return;

        await channel.DeleteMessageAsync(botReplyId);

        var hadError = TryGetError(cachedMessage.Id, out _);
        memoryCache.Remove(AssociatedKey(cachedMessage.Id));
        memoryCache.Remove(ReplyKey(cachedMessage.Id));

        if (hadError)
        {
            var originalMessage = await cachedMessage.GetOrDownloadAsync();

            // If we know what user added the reaction, remove their and our reaction otherwise just remove ours.
            if (reaction.User.IsSpecified) await originalMessage.RemoveReactionAsync(_emote, reaction.User.Value);

            await originalMessage.RemoveReactionAsync(_emote, discordSocketClient.CurrentUser);
        }
    }
}