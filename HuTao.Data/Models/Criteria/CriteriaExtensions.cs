using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Data.Models.Criteria;

public static class CriteriaExtensions
{
    public static bool Judge<T>(this IEnumerable<IJudge<T>> criteria, T target, JudgeType type = JudgeType.Any)
        => type switch
        {
            JudgeType.Any => criteria.Any(c => c.Judge(target)),
            JudgeType.All => criteria.All(c => c.Judge(target)),
            _             => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid judge type.")
        };

    public static bool Judge<T>(this IEnumerable<IJudge<T>> criteria, IEnumerable<T> targets, JudgeType type = JudgeType
        .Any)
        => type switch
        {
            JudgeType.Any => criteria.Any(c => targets.Any(c.Judge)),
            JudgeType.All => criteria.All(c => targets.All(c.Judge)),
            _             => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid judge type.")
        };

    public static bool None<T>(this IEnumerable<IJudge<T>> criteria, T target, JudgeType type = JudgeType.Any)
        => !criteria.Judge(target, type);

    public static bool None<T>(this IEnumerable<IJudge<T>> criteria, IEnumerable<T> targets,
        JudgeType type = JudgeType.Any)
        => !criteria.Judge(targets, type);

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

    public static IEnumerable<AuthorizationGroup> ToAuthorizationGroups(
        this ICriteriaOptions options, AuthorizationScope scope, IGuildUser? moderator = null)
        => options.ToCriteria().Select(criteria
            => new AuthorizationGroup(scope, type: options.JudgeType, rules: criteria).WithModerator(moderator));

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