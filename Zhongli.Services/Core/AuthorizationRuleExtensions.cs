using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Services.Core
{
    public static class AuthorizationRuleExtensions
    {
        public static IEnumerable<T> Scoped<T>(
            this IEnumerable<T> rules, AuthorizationScope scope) where T : AuthorizationGroup
            => rules.Where(rule => (rule.Scope & scope) != 0);

        public static void AddRules(this ICollection<AuthorizationGroup> group,
            AuthorizationScope scope, IGuildUser moderator, ICollection<Criterion> rules)
        {
            group.Add(new AuthorizationGroup(scope, rules).WithModerator(moderator));
        }

        public static void AddRules(this ICollection<AuthorizationGroup> group,
            AuthorizationScope scope, IGuildUser moderator,
            params Criterion[] rules)
        {
            group.Add(new AuthorizationGroup(scope, rules).WithModerator(moderator));
        }

        public static bool Judge(this AuthorizationGroup rules, ICommandContext context, IGuildUser user) =>
            rules.Collection.All(r => r.Judge(context, user));
    }
}