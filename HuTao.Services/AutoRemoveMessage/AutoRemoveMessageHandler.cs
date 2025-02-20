using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HuTao.Services.Core.Messages;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.AutoRemoveMessage;

public class AutoRemoveMessageHandler(
    IRemovableMessageService removable,
    IMemoryCache cache)
    : INotificationHandler<ReactionAddedNotification>,
      INotificationHandler<RemovableMessageRemovedNotification>,
      INotificationHandler<RemovableMessageSentNotification>
{
    private static readonly MemoryCacheEntryOptions MessageCacheOptions =
        new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(60));

    public async Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        var key = GetKey(notification.Message.Id);

        if (cancellationToken.IsCancellationRequested
            || notification.Reaction.Emote.Name != "❌"
            || !cache.TryGetValue(key, out RemovableMessage? cachedMessage) || cachedMessage is null
            || cachedMessage.Users.All(user => user.Id != notification.Reaction.UserId))
            return;

        await cachedMessage.Message.DeleteAsync();

        removable.UnregisterRemovableMessage(cachedMessage.Message);
    }

    public Task Handle(RemovableMessageRemovedNotification notification, CancellationToken cancellationToken)
    {
        var key = GetKey(notification.Message.Id);

        if (cancellationToken.IsCancellationRequested
            || !cache.TryGetValue(key, out _))
            return Task.CompletedTask;

        cache.Remove(key);

        return Task.CompletedTask;
    }

    public Task Handle(RemovableMessageSentNotification notification, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.CompletedTask;

        cache.Set(
            GetKey(notification.Message.Id),
            new RemovableMessage(notification.Message, notification.Users),
            MessageCacheOptions);

        return Task.CompletedTask;
    }

    private static object GetKey(ulong messageId)
        => new
        {
            MessageId = messageId,
            Target    = "RemovableMessage"
        };
}