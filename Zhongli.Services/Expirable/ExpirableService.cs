using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Services.Moderation;

namespace Zhongli.Services.Expirable;

public abstract class ExpirableService<T> where T : class, IExpirable
{
    private readonly IMemoryCache _cache;
    private readonly ZhongliContext _db;

    protected ExpirableService(IMemoryCache cache, ZhongliContext db)
    {
        _cache = cache;
        _db    = db;
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public async Task ExpireEntityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = _cache.GetOrCreate(id, _ => new SemaphoreSlim(1, 1));
        await job.WaitAsync(cancellationToken);

        try
        {
            var expirable = _db.Set<T>().FirstOrDefault(e => e.Id == id);
            if (expirable?.IsActive() is true)
            {
                expirable.EndedAt = DateTimeOffset.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);
                await OnExpiredEntity(expirable, cancellationToken);
            }
        }
        finally
        {
            job.Release();
            _cache.Remove(id);
        }
    }

    public void EnqueueExpirableEntity(T expire, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(expire.Id, out _))
            return;

        if (expire.ExpireAt is null) return;
        var expireAt = expire.ExpireAt.Value;

        BackgroundJob.Schedule(() => ExpireEntityAsync(expire.Id, cancellationToken), expireAt);
        _cache.Set(expire.Id, new SemaphoreSlim(1, 1));
    }

    protected abstract Task OnExpiredEntity(T expired, CancellationToken cancellationToken);
}