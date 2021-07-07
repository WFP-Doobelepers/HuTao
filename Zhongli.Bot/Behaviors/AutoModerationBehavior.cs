using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors
{
    public class AutoModerationBehavior :
        INotificationHandler<WarnNotification>,
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

        public async Task Handle(WarnNotification warn, CancellationToken cancellationToken)
        {
            if (warn.Warning.Type != ModerationActionType.Added)
                return;

            var guildEntity = await _db.Guilds.FindByIdAsync(warn.User.GuildId, cancellationToken);
            var rules = guildEntity?.AutoModerationRules;

            if (rules is null)
                return;

            var userEntity = await _db.Users.TrackUserAsync(warn.User, cancellationToken);
            var currentUser = await warn.Moderator.Guild.GetCurrentUserAsync();
            if (rules.BanTrigger?.IsTriggered(userEntity) ?? false)
            {
                await _moderationService.TryBanAsync(warn.User,  currentUser, rules.BanTrigger.DeleteDays, 
                    "[Warning Trigger]", cancellationToken);
                
                return;
            }

            if (rules.KickTrigger?.IsTriggered(userEntity) ?? false)
            {
                await _moderationService.TryKickAsync(warn.User,  currentUser, 
                    "[Warning Trigger]", cancellationToken);
                
                return;
            }

            var trigger = rules.MuteTriggers
                .Where(t => t.IsTriggered(userEntity))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            if (trigger is not null)
                await _moderationService.TryMuteAsync(warn.User, warn.Moderator, trigger.Length, "[Warning trigger]", cancellationToken);
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