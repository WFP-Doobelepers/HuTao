using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.AutoRemoveMessage;

public class AutoRemoveMessageHandler :
    INotificationHandler<ReactionAddedNotification>,
    INotificationHandler<RemovableMessageRemovedNotification>,
    INotificationHandler<RemovableMessageSentNotification>
{
    private static readonly MemoryCacheEntryOptions MessageCacheOptions =
        new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(60));

    private readonly IAutoRemoveMessageService _autoRemove;
    private readonly IMemoryCache _cache;

    public AutoRemoveMessageHandler(
        IAutoRemoveMessageService autoRemove,
        IMemoryCache cache)
    {
        _autoRemove = autoRemove;
        _cache      = cache;
    }

    public async Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        var key = GetKey(notification.Message.Id);

        if (cancellationToken.IsCancellationRequested
            || notification.Reaction.Emote.Name != "❌"
            || !_cache.TryGetValue(key, out RemovableMessage cachedMessage)
            || cachedMessage.Users.All(user => user.Id != notification.Reaction.UserId))
            return;

        await cachedMessage.Message.DeleteAsync();

        _autoRemove.UnregisterRemovableMessage(cachedMessage.Message);
    }

    public Task Handle(RemovableMessageRemovedNotification notification, CancellationToken cancellationToken)
    {
        var key = GetKey(notification.Message.Id);

        if (cancellationToken.IsCancellationRequested
            || !_cache.TryGetValue(key, out _))
            return Task.CompletedTask;

        _cache.Remove(key);

        return Task.CompletedTask;
    }

    public Task Handle(RemovableMessageSentNotification notification, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.CompletedTask;

        _cache.Set(
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