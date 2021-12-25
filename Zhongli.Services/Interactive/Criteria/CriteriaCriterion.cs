using System.Collections.Generic;
using System.Linq;
using Discord.Commands;

namespace Zhongli.Services.Interactive.Criteria;

public class CriteriaCriterion<T> : ICriterion<T>
{
    public CriteriaCriterion(IEnumerable<ICriterion<T>> criteria) { Criteria = criteria; }

    public CriteriaCriterion(params ICriterion<T>[] criteria) { Criteria = criteria; }

    private CriteriaCriterion(IEnumerable<ICriterion<T>> criteria, params ICriterion<T>[] newCriteria)
    {
        Criteria = criteria.Concat(newCriteria);
    }

    private IEnumerable<ICriterion<T>> Criteria { get; }

    public bool Judge(SocketCommandContext sourceContext, T parameter)
    {
        var judges = Criteria
            .Select(c => c.Judge(sourceContext, parameter));

        return judges.All(r => r);
    }

    public CriteriaCriterion<T> With(ICriterion<T> criterion) => new(Criteria, criterion);
}