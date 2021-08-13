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
        IRequestHandler<ReprimandRequest<Censored>, ReprimandResult>,
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

        public Task<ReprimandResult> Handle(ReprimandRequest<Censored> request, CancellationToken cancellationToken)
            => HandleReprimand(request, cancellationToken);

        public Task<ReprimandResult> Handle(ReprimandRequest<Notice> request, CancellationToken cancellationToken)
            => HandleReprimand(request, cancellationToken);

        public Task<ReprimandResult> Handle(ReprimandRequest<Warning> request, CancellationToken cancellationToken)
            => HandleReprimand(request, cancellationToken);

        private static (string, TriggerSource, ModerationSource) GetDetails<T>(ReprimandRequest<T> request)
            where T : ReprimandAction => request.Reprimand switch
        {
            Censored => ($"[{nameof(Censored)} Count Trigger]", TriggerSource.Censored, ModerationSource.Censor),
            Notice   => ($"[{nameof(Notice)} Count Trigger]", TriggerSource.Notice, ModerationSource.Notice),
            Warning  => ($"[{nameof(Warning)} Count Trigger]", TriggerSource.Warning, ModerationSource.Warning),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request,
                "Unknown kind of reprimand request.")
        };

        private async Task<ITrigger?> TryGetTriggerAsync(ReprimandAction reprimand, uint count, TriggerSource source,
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

        private async Task<ReprimandResult> HandleReprimand<T>(ReprimandRequest<T> request,
            CancellationToken cancellationToken) where T : ReprimandAction
        {
            var ((user, moderator, _, _), reprimand) = request;

            var count = await reprimand.CountAsync(_db, false, cancellationToken);
            var (reason, type, source) = GetDetails(request);
            var trigger = await TryGetTriggerAsync(reprimand, count, type, cancellationToken);

            if (trigger is null) return new ReprimandResult(reprimand);

            var currentUser = await moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(user, currentUser, source, reason);

            var secondary = await _moderation.TryReprimandTriggerAsync(trigger, details, cancellationToken);
            return new ReprimandResult(reprimand, secondary);
        }
    }
}