using System;
using System.Collections.Generic;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public record ReprimandResult(ReprimandAction Reprimand)
    {
        public virtual ReprimandAction Primary { get; } = Reprimand!;

        public virtual IEnumerable<ReprimandAction?> Secondary { get; } = Array.Empty<ReprimandAction?>();
    }

    public record WarningResult(ReprimandAction Warning, ReprimandAction? Reprimand = null)
        : ReprimandResult(Reprimand!)
    {
        public override ReprimandAction Primary { get; } = Warning!;

        public override IEnumerable<ReprimandAction?> Secondary { get; } = new[] { Reprimand };
    }

    public record NoticeResult(ReprimandAction Notice, WarningResult? Result = null)
        : WarningResult(Result?.Warning!, Result?.Reprimand)
    {
        public override ReprimandAction Primary { get; } = Notice;

        public override IEnumerable<ReprimandAction?> Secondary { get; } = new[] { Result?.Warning, Result?.Reprimand };
    }
}