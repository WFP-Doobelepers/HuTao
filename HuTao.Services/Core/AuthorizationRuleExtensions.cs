using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions;

namespace HuTao.Services.Core;

public static class AuthorizationRuleExtensions
{
    public static bool Judge(this AuthorizationGroup rules, Context context) => rules.JudgeType switch
    {
        JudgeType.Any => rules.Collection.Any(r => r.Judge(context)),
        JudgeType.All => rules.Collection.All(r => r.Judge(context)),
        _ => throw new ArgumentOutOfRangeException(nameof(rules.JudgeType), rules.JudgeType, "Invalid Judge type")
    };

    public static IEnumerable<T> Scoped<T>(
        this IEnumerable<T> rules, AuthorizationScope scope) where T : AuthorizationGroup
        => rules.Where(rule => (rule.Scope & scope) != 0 || rule.Scope == AuthorizationScope.All);

    public static void AddRules(
        this ICollection<AuthorizationGroup> group,
        AuthorizationScope scope, IGuildUser moderator,
        AccessType accessType, JudgeType type, params Criterion[] rules)
        => group.Add(new AuthorizationGroup(scope, accessType, type, rules).WithModerator(moderator));
}