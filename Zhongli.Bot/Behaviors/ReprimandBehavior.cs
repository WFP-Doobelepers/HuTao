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

        private static (string, TriggerSource) GetDetails<T>(ReprimandRequest<T> request)
            where T : Reprimand => request.Reprimand switch
        {
            Censored => ($"[{nameof(Censored)} Count Trigger]", TriggerSource.Censored),
            Notice   => ($"[{nameof(Notice)} Count Trigger]", TriggerSource.Notice),
            Warning  => ($"[{nameof(Warning)} Count Trigger]", TriggerSource.Warning),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request,
                "Unknown kind of reprimand request.")
        };

        private async Task<ReprimandResult> HandleReprimand<T>(ReprimandRequest<T> request,
            CancellationToken cancellationToken) where T : Reprimand
        {
            var ((user, moderator, _, _), reprimand) = request;

            var count = await reprimand.CountAsync(_db, false, cancellationToken);
            var (reason, source) = GetDetails(request);
            var trigger = await TryGetTriggerAsync(reprimand, count, source, cancellationToken);

            if (trigger is null) return new ReprimandResult(reprimand);

            var currentUser = await moderator.Guild.GetCurrentUserAsync();
            var details = new ReprimandDetails(user, currentUser, reason, trigger);

            var secondary = await _moderation.TryReprimandTriggerAsync(trigger.Reprimand, details, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new ReprimandResult(reprimand, secondary);
        }

        private async Task<ReprimandTrigger?> TryGetTriggerAsync(Reprimand reprimand, uint count, TriggerSource source,
            CancellationToken cancellationToken)
        {
            var user = await reprimand.GetUserAsync(_db, cancellationToken);
            var rules = user.Guild.ModerationRules;

            return rules.Triggers
                .OfType<ReprimandTrigger>()
                .Where(t => t.Source == source)
                .Where(t => t.IsTriggered(count))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();
        }
    }
}