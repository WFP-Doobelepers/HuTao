using System;
using System.Collections.Generic;
using System.Linq;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class ReprimandResult
    {
        public ReprimandResult(ReprimandAction primary, ReprimandResult? secondary = null)
        {
            Primary = primary;
            Secondary = secondary?.Secondary.Append(secondary.Primary)
                ?? Array.Empty<ReprimandAction>();
        }

        public IEnumerable<ReprimandAction?> Secondary { get; }

        public ReprimandAction Primary { get; }
    }
}