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
        private readonly ModerationService _moderation;

        public ReprimandBehavior(ZhongliContext db, ModerationService moderation)
        {
            _db         = db;
            _moderation = moderation;
        }

        public async Task<NoticeResult> Handle(ReprimandRequest<Notice, NoticeResult> request,
            CancellationToken cancellationToken)
        {
            var ((user, moderator, _, _), reprimand) = request;

            var trigger = await TryGetTriggerAsync(reprimand, await reprimand.CountAsync(_db, cancellationToken),
                rules => rules.NoticeTriggers, cancellationToken);

            if (trigger is null) return new NoticeResult(reprimand);

            var currentUser = await moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(user, currentUser,
                ModerationSource.Notice, "[Notice Trigger]");

            var secondary = await _moderation.WarnAsync(1, details, cancellationToken);
            return new NoticeResult(reprimand, secondary);
        }

        public async Task<WarningResult> Handle(ReprimandRequest<Warning, WarningResult> request,
            CancellationToken cancellationToken)
        {
            var ((user, moderator, _, _), reprimand) = request;

            var trigger = await TryGetTriggerAsync(reprimand, await reprimand.CountAsync(_db, cancellationToken),
                rules => rules.WarningTriggers, cancellationToken);

            if (trigger is null) return new WarningResult(reprimand);

            var currentUser = await moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(user, currentUser,
                ModerationSource.Warning, "[Warning Trigger]");

            ReprimandAction? secondary = trigger switch
            {
                BanTrigger ban => await _moderation.TryBanAsync(ban.DeleteDays, ban.Length, details, cancellationToken),
                KickTrigger => await _moderation.TryKickAsync(details, cancellationToken),
                MuteTrigger mute => await _moderation.TryMuteAsync(mute.Length, details, cancellationToken),
                _ => null
            };

            return new WarningResult(reprimand, secondary);
        }

        private async Task<T?> TryGetTriggerAsync<T>(ReprimandAction reprimand, int count,
            Func<ModerationRules, IEnumerable<T>> selector,
            CancellationToken cancellationToken) where T : ITrigger
        {
            var user = await reprimand.GetUserAsync(_db, cancellationToken);
            var rules = user.Guild.ModerationRules;

            var trigger = selector.Invoke(rules)
                .Where(t => t.IsTriggered(count))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            return trigger;
        }
    }
}