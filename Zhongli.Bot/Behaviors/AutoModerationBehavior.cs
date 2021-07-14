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
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors
{
    public class AutoModerationBehavior :
        INotificationHandler<WarnNotification>,
        INotificationHandler<NoticeNotification>,
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

        public async Task Handle(NoticeNotification notice, CancellationToken cancellationToken)
        {
            var guildEntity = await _db.Guilds.FindByIdAsync(notice.User.Guild.Id, cancellationToken);
            var rules = guildEntity?.AutoModerationRules;

            if (rules is null)
                return;

            var userEntity = await _db.Users.TrackUserAsync(notice.User, cancellationToken);

            var trigger = rules.WarningTriggers.OfType<NoticeTrigger>()
                .Where(t => t.IsTriggered(userEntity.NoticeCount))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            if (trigger is not null)
            {
                var currentUser = await notice.Moderator.Guild.GetCurrentUserAsync();
                var details = new ReprimandDetails(notice.User, currentUser,
                    ModerationSource.Auto, "[Notice Trigger]");
                await _moderationService.WarnAsync(1, details, cancellationToken);
            }
        }

        public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            _mutesProcessor ??= Task.Factory.StartNew(() => ProcessMutes(cancellationToken), cancellationToken,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Task.CompletedTask;
        }

        public async Task Handle(WarnNotification warn, CancellationToken cancellationToken)
        {
            var guildEntity = await _db.Guilds.FindByIdAsync(warn.User.Guild.Id, cancellationToken);
            var rules = guildEntity?.AutoModerationRules;

            if (rules is null)
                return;

            var userEntity = await _db.Users.TrackUserAsync(warn.User, cancellationToken);
            var currentUser = await warn.Moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(warn.User, currentUser,
                ModerationSource.Auto, "[Warning Trigger]");

            foreach (var trigger in rules.WarningTriggers
                .Where(t => t.IsTriggered(userEntity.WarningCount))
                .OrderByDescending(t => t.Amount))
            {
                switch (trigger)
                {
                    case BanTrigger ban:
                        await _moderationService.TryBanAsync(ban.DeleteDays, details, cancellationToken);
                        return;
                    case KickTrigger:
                        await _moderationService.TryKickAsync(details, cancellationToken);
                        return;
                    case MuteTrigger mute:
                        await _moderationService.TryMuteAsync(mute.Length, details, cancellationToken);
                        return;
                }
            }
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