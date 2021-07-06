using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using GuildPermission = Zhongli.Data.Models.Authorization.GuildPermission;

namespace Zhongli.Services.Core
{
    public static class AuthorizationRuleExtensions
    {
        public static IEnumerable<T> Scoped<T>(
            this IEnumerable<T> rules, AuthorizationScope scope) where T : AuthorizationGroup
            => rules.Where(rule => (rule.Scope & scope) != 0);

        public static void AddRules(this ICollection<AuthorizationGroup> group,
            AuthorizationScope scope, IGuildUser moderator, ICollection<AuthorizationRule> rules)
        {
            group.Add(new AuthorizationGroup(scope, moderator, rules));
        }

        public static void AddRules(this ICollection<AuthorizationGroup> group,
            AuthorizationScope scope, IGuildUser moderator,
            params AuthorizationRule[] rules)
        {
            group.Add(new AuthorizationGroup(scope, moderator, rules));
        }

        public static bool IsAuthorized(this AuthorizationGroup rules, ICommandContext context, IGuildUser user) =>
            rules.Collection.All(r => r.IsAuthorized(context, user));

        private static bool IsAuthorized(this AuthorizationRule rule, ICommandContext context, IGuildUser user)
        {
            return rule switch
            {
                UserAuthorization auth       => auth.IsAuthorized(user),
                RoleAuthorization auth       => auth.IsAuthorized(user),
                PermissionAuthorization auth => auth.IsAuthorized(user),
                ChannelAuthorization
                    { IsCategory: true } auth => auth.IsAuthorized(((ITextChannel) context.Channel).CategoryId),
                ChannelAuthorization auth => auth.IsAuthorized((ITextChannel) context.Channel),
                GuildAuthorization    => true,
                _                         => false
            };
        }

        private static bool IsAuthorized(this UserAuthorization auth, IGuildUser user)
            =>  auth.UserId == user.Id;

        private static bool IsAuthorized(this RoleAuthorization auth, IGuildUser user)
            => user.RoleIds.Contains(auth.RoleId);

        private static bool IsAuthorized(this PermissionAuthorization auth, IGuildUser user)
            => (auth.Permission & (GuildPermission) user.GuildPermissions.RawValue) != 0;

        private static bool IsAuthorized(this ChannelAuthorization auth, ITextChannel channel)
            => auth.ChannelId == channel.Id;

        private static bool IsAuthorized(this ChannelAuthorization auth, ulong? categoryId)
            => auth.ChannelId == categoryId;
    }
}