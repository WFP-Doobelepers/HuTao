using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors
{
    public class AutoModerationBehavior :
        INotificationHandler<WarnNotification>,
        INotificationHandler<ReadyNotification>
    {
        private readonly ZhongliContext _db;
        private readonly ModerationService _moderationService;
        private Task? _mutesProcessor;

        public AutoModerationBehavior(ZhongliContext db, ModerationService moderationService)
        {
            _db                = db;
            _moderationService = moderationService;
        }

        public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            if (_mutesProcessor is not null)
                return;

            _mutesProcessor = Task.Factory.StartNew(() => ProcessMutes(cancellationToken), cancellationToken,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task Handle(WarnNotification warn, CancellationToken cancellationToken)
        {
            if (warn.Warning.Type != ModerationActionType.Added)
                return;
            
            var guildEntity = await _db.Guilds.FindByIdAsync(warn.User.GuildId, cancellationToken);
            var rules = guildEntity?.AutoModerationRules;

            if (rules is null)
                return;

            var userEntity = await _db.Users.TrackUserAsync(warn.User, cancellationToken);
            if (rules.BanTrigger?.IsTriggered(userEntity) ?? false)
            {
                await warn.User.BanAsync(0, "[Auto Trigger]");
                return;
            }

            if (rules.KickTrigger?.IsTriggered(userEntity) ?? false)
            {
                await warn.User.KickAsync("[Auto Trigger]");
                return;
            }

            var trigger = rules.MuteTriggers
                .Where(t => t.IsTriggered(userEntity))
                .OrderByDescending(t => t.TriggerAt)
                .FirstOrDefault();

            if (trigger is not null)
                await _moderationService.TryMuteAsync(warn.User, warn.Moderator,
                    "[Auto trigger]", trigger.Length, cancellationToken);
        }

        private async Task ProcessMutes(CancellationToken cancellationToken)
        {
            while (true)
            {
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