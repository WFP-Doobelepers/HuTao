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
            this IEnumerable<T> rules, AuthorizationScope scope) where T : AuthorizationRule
            => rules.Where(rule => (rule.Scope & scope) != 0);

        public static IEnumerable<AuthorizationRule> Scoped(
            this AuthorizationRules rules, AuthorizationScope scope)
            => rules.UserAuthorizations.Scoped<AuthorizationRule>(scope)
                .Concat(rules.RoleAuthorizations).Scoped(scope)
                .Concat(rules.PermissionAuthorizations).Scoped(scope)
                .Concat(rules.ChannelAuthorizations).Scoped(scope)
                .Concat(rules.GuildAuthorizations).Scoped(scope);

        public static bool IsAuthorized(this AuthorizationGroup group, ICommandContext context, IGuildUser user) =>
            group.Collection.All(r => r.IsAuthorized(context, user));

        public static bool IsAuthorized(this AuthorizationRule rule, ICommandContext context, IGuildUser user)
        {
            return rule switch
            {
                UserAuthorization auth       => auth.IsAuthorized(user),
                RoleAuthorization auth       => auth.IsAuthorized(user),
                PermissionAuthorization auth => auth.IsAuthorized(user),
                ChannelAuthorization
                    { IsCategory: true } auth => auth.IsAuthorized(((ITextChannel) context.Channel).CategoryId),
                ChannelAuthorization auth => auth.IsAuthorized((ITextChannel) context.Channel),
                GuildAuthorization auth   => auth.IsAuthorized(user),
                _                         => false
            };
        }

        public static bool IsAuthorized(this UserAuthorization auth, IGuildUser user)
            => auth.GuildId == user.GuildId && auth.UserId == user.Id;

        public static bool IsAuthorized(this RoleAuthorization auth, IGuildUser user)
            => user.RoleIds.Contains(auth.RoleId);

        public static bool IsAuthorized(this PermissionAuthorization auth, IGuildUser user)
            => (auth.Permission & (GuildPermission) user.GuildPermissions.RawValue) != 0;

        public static bool IsAuthorized(this ChannelAuthorization auth, ITextChannel channel)
            => auth.ChannelId == channel.Id;

        public static bool IsAuthorized(this ChannelAuthorization auth, ulong? categoryId)
            => auth.ChannelId == categoryId;

        public static bool IsAuthorized(this GuildAuthorization auth, IGuildUser user)
            => auth.GuildId == user.GuildId;
    }
}