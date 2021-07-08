using System.Linq;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Criteria;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Services.Core
{
    public static class CriteriaExtensions
    {
        public static bool Judge(this Criterion rule, ICommandContext context, IGuildUser user)
            => Judge(rule, (ITextChannel) context.Channel, user);

        public static bool Judge(this Criterion rule, ITextChannel channel, IGuildUser user)
        {
            return rule switch
            {
                UserCriterion auth       => auth.Judge(user),
                RoleCriterion auth       => auth.Judge(user),
                PermissionCriterion auth => auth.Judge(user),
                ChannelCriterion
                    { IsCategory: true } auth => auth.Judge(channel.CategoryId),
                ChannelCriterion auth => auth.Judge(channel.Id),
                GuildCriterion        => true,
                _                     => false
            };
        }

        public static bool Judge(this UserCriterion auth, IGuildUser user)
            => auth.UserId == user.Id;

        public static bool Judge(this RoleCriterion auth, IGuildUser user)
            => user.RoleIds.Contains(auth.RoleId);

        public static bool Judge(this PermissionCriterion auth, IGuildUser user)
            => (auth.Permission & (GuildPermission) user.GuildPermissions.RawValue) != 0;

        public static bool Judge(this ChannelCriterion auth, ITextChannel channel)
            => auth.ChannelId == channel.Id;

        public static bool Judge(this ChannelCriterion auth, ulong? categoryId)
            => auth.ChannelId == categoryId;
    }
}