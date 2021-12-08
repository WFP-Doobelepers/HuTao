using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Services.Moderation;

namespace Zhongli.Services.Expirable
{
    public abstract class ExpirableService<T> where T : class, IExpirable
    {
        private readonly ZhongliContext _db;

        protected ExpirableService(ZhongliContext db) { _db = db; }

        // ReSharper disable once MemberCanBePrivate.Global
        public async Task ExpireEntityAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var expirable = _db.Set<T>().FirstOrDefault(e => e.Id == id);
            if (expirable?.IsActive() is true)
            {
                expirable.EndedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                await OnExpiredEntity(expirable, cancellationToken);
            }
        }

        public void EnqueueExpirableEntity(T expire, CancellationToken cancellationToken = default)
        {
            if (expire.ExpireAt is not null)
            {
                BackgroundJob.Schedule(()
                        => ExpireEntityAsync(expire.Id, cancellationToken),
                    expire.ExpireAt.Value);
            }
        }

        protected abstract Task OnExpiredEntity(T expired, CancellationToken cancellationToken);
    }
}