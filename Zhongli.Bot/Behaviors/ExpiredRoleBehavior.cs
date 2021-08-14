using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors
{
    public class ExpiredRoleBehavior : INotificationHandler<ReadyNotification>
    {
        private readonly TemporaryRoleService _expire;
        private readonly ZhongliContext _db;

        public ExpiredRoleBehavior(TemporaryRoleService expire, ZhongliContext db)
        {
            _expire = expire;
            _db     = db;
        }

        public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            var active = _db.Set<TemporaryRole>().AsAsyncEnumerable()
                .Where(m => m.IsActive());

            await foreach (var entity in active.WithCancellation(cancellationToken))
            {
                _expire.EnqueueExpirableRole(entity, cancellationToken);
            }
        }
    }
}