using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Zhongli.Data;
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
            var activeMutes = _db.MuteHistory.AsAsyncEnumerable()
                .Where(m => m.IsActive);

            await foreach (var mute in activeMutes.WithCancellation(cancellationToken))
            {
                BackgroundJob.Schedule(() => _moderationService.ExpireMuteAsync(mute.Id, cancellationToken),
                    mute.TimeLeft!.Value);
            }
        }
    }
}