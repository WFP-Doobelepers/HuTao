using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Core.Messages;
using HuTao.Services.Moderation;
using MediatR;

namespace HuTao.Services.Expirable;

public class ExpiredEntityBehavior<T>(HuTaoContext db, ExpirableService<T> expire)
    : INotificationHandler<ReadyNotification> where T : class, IExpirable
{
    public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        var active = db.Set<T>().AsAsyncEnumerable()
            .Where(m => m.IsActive());

        await foreach (var entity in active.WithCancellation(cancellationToken))
        {
            expire.EnqueueExpirableEntity(entity, cancellationToken);
        }
    }
}