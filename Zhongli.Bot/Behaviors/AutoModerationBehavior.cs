using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Bot.Behaviors
{
    public class AutoModerationBehavior :
        IRequestHandler<ReprimandRequest<Warning>, ReprimandAction>,
        IRequestHandler<ReprimandRequest<Notice>, ReprimandAction>,
        INotificationHandler<ReadyNotification>
    {
        private static Task? _mutesProcessor;
        private readonly ZhongliContext _db;
        private readonly ModerationService _moderationService;

        public AutoModerationBehavior(ZhongliContext db, ModerationService moderationService)
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

        public async Task<ReprimandAction> Handle(ReprimandRequest<Notice> reprimand,
            CancellationToken cancellationToken)
        {
            var rules = reprimand.Reprimand.Guild.AutoModerationRules;
            var notices = reprimand.Reprimand.User.HistoryCount<Notice>();

            var trigger = rules.NoticeTriggers
                .Where(t => t.IsTriggered(notices))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            if (trigger is null) return reprimand.Reprimand;

            var currentUser = await reprimand.Moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(reprimand.User, currentUser,
                ModerationSource.Auto, "[Notice Trigger]");

            return await _moderationService.WarnAsync(1, details, cancellationToken);
        }

        public async Task<ReprimandAction> Handle(ReprimandRequest<Warning> reprimand,
            CancellationToken cancellationToken)
        {
            var rules = reprimand.Reprimand.Guild.AutoModerationRules;
            var warnings = reprimand.Reprimand.User.ReprimandCount<Warning>();
            var currentUser = await reprimand.Moderator.Guild.GetCurrentUserAsync();

            var details = new ReprimandDetails(reprimand.User, currentUser,
                ModerationSource.Auto, "[Warning Trigger]");

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

            return action ?? reprimand.Reprimand;
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