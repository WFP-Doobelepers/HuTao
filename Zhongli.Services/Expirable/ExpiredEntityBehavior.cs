using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;

namespace Zhongli.Services.Expirable
{
    public class ExpiredEntityBehavior<T> : INotificationHandler<ReadyNotification>
        where T : class, IExpirable
    {
        private readonly ExpirableService<T> _expire;
        private readonly ZhongliContext _db;

        public ExpiredEntityBehavior(ZhongliContext db, ExpirableService<T> expire)
        {
            _db     = db;
            _expire = expire;
        }

        public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            var active = _db.Set<T>().AsAsyncEnumerable()
                .Where(m => m.IsActive());

            await foreach (var entity in active.WithCancellation(cancellationToken))
            {
                _expire.EnqueueExpirableEntity(entity, cancellationToken);
            }
        }
    }
}