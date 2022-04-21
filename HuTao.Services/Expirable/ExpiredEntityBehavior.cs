using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Core.Messages;
using HuTao.Services.Moderation;
using MediatR;

namespace HuTao.Services.Expirable;

public class ExpiredEntityBehavior<T> : INotificationHandler<ReadyNotification>
    where T : class, IExpirable
{
    private readonly ExpirableService<T> _expire;
    private readonly HuTaoContext _db;

    public ExpiredEntityBehavior(HuTaoContext db, ExpirableService<T> expire)
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