using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Moderation;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Expirable;

public abstract class ExpirableService<T>(IMemoryCache cache, HuTaoContext db)
    where T : class, IExpirable
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public async Task ExpireEntityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = cache.GetOrCreate(id, _ => new SemaphoreSlim(1, 1))!;

        try
        {
            await job.WaitAsync(cancellationToken);

            var expirable = db.Set<T>().FirstOrDefault(e => e.Id == id);
            if (expirable?.IsActive() is true)
            {
                expirable.EndedAt = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync(cancellationToken);
                await OnExpiredEntity(expirable, cancellationToken);
            }
        }
        finally
        {
            job.Release();
            cache.Remove(id);
        }
    }

    public void EnqueueExpirableEntity(T expire, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(expire.Id, out _))
            return;

        if (expire.ExpireAt is null) return;
        var expireAt = expire.ExpireAt.Value;

        BackgroundJob.Schedule(() => ExpireEntityAsync(expire.Id, cancellationToken), expireAt);
        cache.Set(expire.Id, new SemaphoreSlim(1, 1));
    }

    protected abstract Task OnExpiredEntity(T expired, CancellationToken cancellationToken);
}