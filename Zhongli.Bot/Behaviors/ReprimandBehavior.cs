using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors
{
    public class ReprimandBehavior :
        IRequestHandler<ReprimandRequest<Warning, WarningResult>, WarningResult>,
        IRequestHandler<ReprimandRequest<Notice, NoticeResult>, NoticeResult>,
        INotificationHandler<ReadyNotification>
    {
        private static Task? _mutesProcessor;
        private readonly ZhongliContext _db;
        private readonly ModerationService _moderationService;

        public ReprimandBehavior(ZhongliContext db, ModerationService moderationService)
        {
            _db                = db;
            _moderationService = moderationService;
        }

        public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            _mutesProcessor ??= Task.Factory.StartNew(() => ProcessMutes(cancellationToken), cancellationToken,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Task.CompletedTask;
        }

        public async Task<NoticeResult> Handle(ReprimandRequest<Notice, NoticeResult> request,
            CancellationToken cancellationToken)
        {
            var reprimand = request.Reprimand;
            var user = await reprimand.GetUserAsync(_db, cancellationToken);

            var rules = user.Guild.AutoModerationRules;
            var notices = user.HistoryCount<Notice>();

            var trigger = rules.NoticeTriggers
                .Where(t => t.IsTriggered(notices))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            if (trigger is null) return new NoticeResult(request.Reprimand);

            var currentUser = await request.Moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(request.User, currentUser,
                ModerationSource.Notice, "[Notice Trigger]");

            return new NoticeResult(request.Reprimand,
                await _moderationService.WarnAsync(1, details, cancellationToken));
        }

        public async Task<WarningResult> Handle(ReprimandRequest<Warning, WarningResult> request,
            CancellationToken cancellationToken)
        {
            var reprimand = request.Reprimand;
            var user = await reprimand.GetUserAsync(_db, cancellationToken);

            var rules = user.Guild.AutoModerationRules;
            var warnings = user.ReprimandCount<Warning>();

            var currentUser = await request.Moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(request.User, currentUser,
                ModerationSource.Warning, "[Warning Trigger]");

            var trigger = rules.WarningTriggers
                .Where(t => t.IsTriggered(warnings))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            ReprimandAction? action = trigger switch
            {
                BanTrigger ban   => await _moderationService.TryBanAsync(ban.DeleteDays, details, cancellationToken),
                KickTrigger      => await _moderationService.TryKickAsync(details, cancellationToken),
                MuteTrigger mute => await _moderationService.TryMuteAsync(mute.Length, details, cancellationToken)
            };

            return new WarningResult(reprimand, action);
        }

        private async Task ProcessMutes(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var now = DateTimeOffset.Now;
                var activeMutes = await _db.Set<Mute>()
                    .AsAsyncEnumerable()
                    .Where(m => m.EndedAt == null)
                    .Where(m => m.StartedAt + m.Length > now)
                    .Where(m => m.StartedAt + m.Length - now < TimeSpan.FromMinutes(10))
                    .ToListAsync(cancellationToken);

                foreach (var mute in activeMutes)
                {
                    _ = _moderationService.EnqueueMuteTimer(mute, cancellationToken);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }
    }
}