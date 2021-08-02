using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors
{
    public class ExpiredReprimandBehavior : INotificationHandler<ReadyNotification>
    {
        private readonly ZhongliContext _db;
        private readonly ModerationService _moderation;

        public ExpiredReprimandBehavior(ZhongliContext db, ModerationService moderation)
        {
            _db         = db;
            _moderation = moderation;
        }

        public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            var active = _db.Set<ReprimandAction>().AsAsyncEnumerable()
                .OfType<IExpire>()
                .Where(m => m.IsActive());

            await foreach (var entity in active.WithCancellation(cancellationToken))
            {
                var timeLeft = entity.TimeLeft();
                if (timeLeft is null) continue;

                BackgroundJob.Schedule(()
                        => _moderation.ExpireReprimandAsync(entity.Id, cancellationToken),
                    timeLeft.Value);
            }
        }
    }
}