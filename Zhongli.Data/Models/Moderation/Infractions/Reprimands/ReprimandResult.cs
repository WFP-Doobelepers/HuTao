using System;
using System.Collections.Generic;
using System.Linq;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands;

public class ReprimandResult
{
    public ReprimandResult(Reprimand primary, ReprimandResult? secondary = null)
    {
        Primary = primary;
        Secondary = secondary?.Secondary.Append(secondary.Primary)
            ?? Array.Empty<Reprimand>();
    }

    public IEnumerable<Reprimand?> Secondary { get; }

    public Reprimand Primary { get; }
}