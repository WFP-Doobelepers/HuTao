using System;
using System.Collections.Concurrent;
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
    private const string AssociatedErrorsKey = nameof(CommandErrorHandler) + ".AssociatedErrors";
    private const string ErrorRepliesKey = nameof(CommandErrorHandler) + ".ErrorReplies";
    private const string Emoji = "⚠";
    private readonly IEmote _emote = new Emoji(Emoji);

    private ConcurrentDictionary<ulong, string> AssociatedErrors =>
        memoryCache.GetOrCreate(AssociatedErrorsKey, _ => new ConcurrentDictionary<ulong, string>())
        ?? throw new InvalidOperationException($"Cache returned null for {nameof(AssociatedErrors)}");

    private ConcurrentDictionary<ulong, ulong> ErrorReplies =>
        memoryCache.GetOrCreate(ErrorRepliesKey, _ => new ConcurrentDictionary<ulong, ulong>())
        ?? throw new InvalidOperationException($"Cache returned null for {nameof(ErrorReplies)}");

    public async Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
        => await ReactionAdded(notification.Message, await notification.Channel.GetOrDownloadAsync(),
            notification.Reaction);

    public async Task Handle(
        ReactionRemovedNotification notification,
        CancellationToken cancellationToken)
        => await ReactionRemoved(notification.Message, await notification.Channel.GetOrDownloadAsync(),
            notification.Reaction);

    public async Task AssociateError(IUserMessage message, string error)
    {
        if (AssociatedErrors.TryAdd(message.Id, error)) await message.AddReactionAsync(new Emoji(Emoji));
    }

    public Task AssociateError(Context context, string error) => context switch
    {
        CommandContext c => AssociateError(c.Message, error),
        _                => context.ReplyAsync(error, ephemeral: true)
    };

    private async Task ReactionAdded(
        Cacheable<IUserMessage, ulong> cachedMessage, IMessageChannel channel,
        SocketReaction reaction)
    {
        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;
        if (reaction.Emote.Name != Emoji || ErrorReplies.ContainsKey(cachedMessage.Id)) return;

        var message = await cachedMessage.GetOrDownloadAsync();
        if (message.Author.Id != reaction.User.Value.Id) return;

        if (AssociatedErrors.TryGetValue(cachedMessage.Id, out var value))
        {
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

            var msg = await channel.SendMessageAsync(
                components: components,
                allowedMentions: AllowedMentions.None);

            if (ErrorReplies.TryAdd(cachedMessage.Id, msg.Id) == false) await msg.DeleteAsync();
        }
    }

    private async Task ReactionRemoved(
        Cacheable<IUserMessage, ulong> cachedMessage, IMessageChannel channel,
        SocketReaction reaction)
    {
        if (!reaction.User.IsSpecified || reaction.User.Value is null) return;
        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;
        if (reaction.Emote.Name != Emoji) return;

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

            await originalMessage.RemoveReactionAsync(_emote, discordSocketClient.CurrentUser);
        }
    }
}