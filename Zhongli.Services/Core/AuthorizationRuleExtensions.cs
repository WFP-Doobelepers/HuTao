using System.Collections.Generic;
using System.Linq;
using Discord;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Services.Core;

public static class AuthorizationRuleExtensions
{
    public static bool Judge(this AuthorizationGroup rules, Context context) =>
        rules.Collection.Any(r => r.Judge(context));

    public static IEnumerable<T> Scoped<T>(
        this IEnumerable<T> rules, AuthorizationScope scope) where T : AuthorizationGroup
        => rules.Where(rule => (rule.Scope & scope) != 0);

    public static void AddRules(this ICollection<AuthorizationGroup> group,
        AuthorizationScope scope, IGuildUser moderator, AccessType accessType,
        params Criterion[] rules)
        => group.Add(new AuthorizationGroup(scope, accessType, rules).WithModerator(moderator));
}