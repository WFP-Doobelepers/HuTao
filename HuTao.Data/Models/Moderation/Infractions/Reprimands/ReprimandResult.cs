using System.Collections.Generic;
using System.Linq;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public class ReprimandResult
{
    public ReprimandResult(Reprimand reprimand, ReprimandResult? result)
    {
        if (result is null)
            Primary = reprimand;
        else
        {
            Primary   = result.Primary;
            Secondary = result.Secondary.Append(reprimand);
        }
    }

    public ReprimandResult(Reprimand primary) { Primary = primary; }

    public IEnumerable<Reprimand> Secondary { get; } = Enumerable.Empty<Reprimand>();

    public Reprimand Last => Secondary.LastOrDefault() ?? Primary;

    public Reprimand Primary { get; }
}