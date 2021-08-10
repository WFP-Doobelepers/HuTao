using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors
{
    public class ReprimandBehavior :
        IRequestHandler<ReprimandRequest<Notice>, ReprimandResult>,
        IRequestHandler<ReprimandRequest<Warning>, ReprimandResult>
    {
        private readonly ModerationService _moderation;
        private readonly ZhongliContext _db;

        public ReprimandBehavior(ModerationService moderation, ZhongliContext db)
        {
            _moderation = moderation;
            _db         = db;
        }

        public Task<ReprimandResult> Handle(ReprimandRequest<Notice> request, CancellationToken cancellationToken)
            => HandleReprimand(request, cancellationToken);

        public Task<ReprimandResult> Handle(ReprimandRequest<Warning> request, CancellationToken cancellationToken)
            => HandleReprimand(request, cancellationToken);

        private static (string Reason, TriggerSource Trigger, ModerationSource Source) GetDetails<T>(
            ReprimandRequest<T> request) where T : ReprimandAction
            => request.Reprimand switch
            {
                Notice  => ("[Notice Trigger]", TriggerSource.Notice, ModerationSource.Notice),
                Warning => ("[Warning Trigger]", TriggerSource.Warning, ModerationSource.Warning),
                _ => throw new ArgumentOutOfRangeException(nameof(request), request,
                    "Unknown kind of reprimand request.")
            };

        private async Task<ITrigger?> TryGetTriggerAsync(ReprimandAction reprimand,
            uint count, TriggerSource source,
            CancellationToken cancellationToken)
        {
            var user = await reprimand.GetUserAsync(_db, cancellationToken);
            var rules = user.Guild.ModerationRules;

            return rules.Triggers
                .Where(t => t.Source == source)
                .Where(t => t.IsTriggered(count))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();
        }

        private async Task<ReprimandResult?> TrySecondaryReprimandAsync(ITrigger? trigger, ReprimandDetails details,
            CancellationToken cancellationToken)
            => trigger switch
            {
                BanTrigger b   => await _moderation.TryBanAsync(b.DeleteDays, b.Length, details, cancellationToken),
                KickTrigger    => await _moderation.TryKickAsync(details, cancellationToken),
                MuteTrigger m  => await _moderation.TryMuteAsync(m.Length, details, cancellationToken),
                WarningTrigger => await _moderation.WarnAsync(1, details, cancellationToken),
                NoticeTrigger  => await _moderation.NoticeAsync(details, cancellationToken),
                _              => null
            };

        private async Task<ReprimandResult> HandleReprimand<T>(ReprimandRequest<T> request,
            CancellationToken cancellationToken) where T : ReprimandAction
        {
            var ((user, moderator, _, _), reprimand) = request;

            var count = await reprimand.CountAsync(_db, cancellationToken);
            var (reason, type, source) = GetDetails(request);
            var trigger = await TryGetTriggerAsync(reprimand, count, type, cancellationToken);

            if (trigger is null) return new ReprimandResult(reprimand);

            var currentUser = await moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(user, currentUser, source, reason);

            var secondary = await TrySecondaryReprimandAsync(trigger, details, cancellationToken);
            return new ReprimandResult(reprimand, secondary);
        }
    }
}