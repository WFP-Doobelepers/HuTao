using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive.Criteria;
using Discord.Commands;

namespace Zhongli.Services.Interactive.Criteria
{
    public class CriteriaCriterion<T> : ICriterion<T>
    {
        public CriteriaCriterion(IEnumerable<ICriterion<T>> criteria) { Criteria = criteria; }

        public CriteriaCriterion(params ICriterion<T>[] criteria) { Criteria = criteria; }

        private CriteriaCriterion(IEnumerable<ICriterion<T>> criteria, params ICriterion<T>[] newCriteria)
        {
            Criteria = criteria.Concat(newCriteria);
        }

        private IEnumerable<ICriterion<T>> Criteria { get; }

        public async Task<bool> JudgeAsync(SocketCommandContext sourceContext, T parameter)
        {
            var judges = Criteria
                .Select(c => c.JudgeAsync(sourceContext, parameter));

            var results = await Task.WhenAll(judges);

            return results.All(r => r);
        }

        public CriteriaCriterion<T> With(ICriterion<T> criterion) => new(Criteria, criterion);
    }
}