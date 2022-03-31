using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Data.Models.Criteria;

public static class CriteriaExtensions
{
    public static ICollection<Criterion> AddCriteria(this ICollection<Criterion> collection,
        ICriteriaOptions options)
    {
        var channels = options.Channels?.Where(c => c is ICategoryChannel or ITextChannel);

        var rules = collection
            .AddCriteria(options.Users, u => new UserCriterion(u.Id))
            .AddCriteria(channels, c => new ChannelCriterion(c.Id, c is ICategoryChannel))
            .AddCriteria(options.Roles, r => new RoleCriterion(r));

        if (options.Permission is not GuildPermission.None)
            rules.Add(new PermissionCriterion(options.Permission));

        return rules;
    }

    public static ICollection<Criterion> ToCriteria(this ICriteriaOptions options)
        => new List<Criterion>().AddCriteria(options);

    private static ICollection<Criterion> AddCriteria<T>(this ICollection<Criterion> collection,
        IEnumerable<T>? source, Func<T, Criterion> func)
    {
        if (source is null)
            return collection;

        foreach (var item in source)
        {
            collection.Add(func(item));
        }

        return collection;
    }
}