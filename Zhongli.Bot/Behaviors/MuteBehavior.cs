using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors
{
    public class MuteBehavior : INotificationHandler<ReadyNotification>
    {
        private readonly ZhongliContext _db;
        private readonly ModerationService _moderationService;

        public MuteBehavior(ZhongliContext db, ModerationService moderationService)
        {
            _db                = db;
            _moderationService = moderationService;
        }

        public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.Now;
            var activeMutes = _db.Set<Mute>()
                .AsAsyncEnumerable()
                .Where(m => m.EndedAt == null)
                .Where(m => m.StartedAt + m.Length > now)
                .Where(m => m.TimeLeft is not null);

            await foreach (var mute in activeMutes.WithCancellation(cancellationToken))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(mute.TimeLeft!.Value, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                        await _moderationService.UnmuteAsync(mute, cancellationToken);
                }, cancellationToken);
            }
        }
    }
}