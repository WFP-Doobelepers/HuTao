using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Zhongli.Services.Interactive.TryParse;
using Zhongli.Services.Interactive.TypeReaders;

namespace Zhongli.Services.Interactive.Criteria;

public static class CriteriaExtensions
{
    public static CriteriaCriterion<T> AsCriterion<T>(this IEnumerable<ICriterion<T>> criteria) =>
        new(criteria);

    public static CriteriaCriterion<T> AsCriterion<T>(this ICriterion<T> criteria) =>
        new(criteria);

    public static Func<T, bool> AsFunc<T>(this ICriterion<T> criteria, SocketCommandContext context)
        => compare => criteria.Judge(context, compare);

    public static IEnumerable<ICriterion<IMessage>> GetCriteria(this IPromptCriteria promptCriteria)
    {
        var criteria = new List<ICriterion<IMessage>>();

        if (promptCriteria.Criteria is not null)
            criteria.AddRange(promptCriteria.Criteria);

        if (promptCriteria.TypeReader is not null)
            criteria.Add(promptCriteria.TypeReader.AsCriterion());

        return criteria;
    }

    public static Predicate<T> AsPredicate<T>(this ICriterion<T> criteria, SocketCommandContext context)
        => compare => criteria.Judge(context, compare);

    public static TryParseCriterion<T> AsCriterion<T>(this TryParseDelegate<T> tryParse) =>
        new(tryParse);
}