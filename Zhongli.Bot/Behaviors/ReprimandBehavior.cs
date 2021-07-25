using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors
{
    public class ReprimandBehavior :
        IRequestHandler<ReprimandRequest<Warning, WarningResult>, WarningResult>,
        IRequestHandler<ReprimandRequest<Notice, NoticeResult>, NoticeResult>
    {
        private readonly ZhongliContext _db;
        private readonly ModerationService _moderationService;

        public ReprimandBehavior(ZhongliContext db, ModerationService moderationService)
        {
            _db                = db;
            _moderationService = moderationService;
        }

        public async Task<NoticeResult> Handle(ReprimandRequest<Notice, NoticeResult> request,
            CancellationToken cancellationToken)
        {
            var (guildUser, moderator, reprimand) = request;

            var trigger = await TryGetTriggerAsync(reprimand,
                rules => rules.NoticeTriggers, cancellationToken);

            if (trigger is null) return new NoticeResult(reprimand);

            var currentUser = await moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(guildUser, currentUser,
                ModerationSource.Notice, "[Notice Trigger]");

            var secondary = await _moderationService.WarnAsync(1, details, cancellationToken);
            return new NoticeResult(reprimand, secondary);
        }

        public async Task<WarningResult> Handle(ReprimandRequest<Warning, WarningResult> request,
            CancellationToken cancellationToken)
        {
            var (guildUser, moderator, reprimand) = request;

            var trigger = await TryGetTriggerAsync(reprimand,
                rules => rules.WarningTriggers, cancellationToken);

            if (trigger is null) return new WarningResult(reprimand);

            var currentUser = await moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(guildUser, currentUser,
                ModerationSource.Warning, "[Warning Trigger]");

            ReprimandAction? secondary = trigger switch
            {
                BanTrigger ban   => await _moderationService.TryBanAsync(ban.DeleteDays, details, cancellationToken),
                KickTrigger      => await _moderationService.TryKickAsync(details, cancellationToken),
                MuteTrigger mute => await _moderationService.TryMuteAsync(mute.Length, details, cancellationToken),
                _                => null
            };

            return new WarningResult(reprimand, secondary);

        }

        private async Task<T?> TryGetTriggerAsync<T>(ReprimandAction reprimand,
            Func<AutoModerationRules, IEnumerable<T>> selector,
            CancellationToken cancellationToken) where T : ITrigger
        {
            var user = await reprimand.GetUserAsync(_db, cancellationToken);
            var rules = user.Guild.AutoModerationRules;
            var warnings = user.ReprimandCount<Warning>();

            var trigger = selector.Invoke(rules)
                .Where(t => t.IsTriggered(warnings))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            return trigger;
        }
    }
}