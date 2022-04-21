using System;
using System.Linq;
using Discord;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Services.Core;

public static class CriteriaExtensions
{
    public static bool Judge(this Criterion rule, Context context)
        => context.User is IGuildUser user
            && context.Channel is INestedChannel channel
            && Judge(rule, channel, user);

    public static bool Judge(this Criterion rule, INestedChannel channel, IGuildUser user) => rule switch
    {
        UserCriterion auth       => auth.Judge(user),
        RoleCriterion auth       => auth.Judge(user),
        PermissionCriterion auth => auth.Judge(user),
        ChannelCriterion auth    => auth.Judge(channel),
        _                        => false
    };

    public static EmbedBuilder ToEmbedBuilder(this Criterion criterion) => new EmbedBuilder()
        .WithTitle($"{criterion.Id}")
        .WithDescription($"{criterion}");

    public static Type GetCriterionType(this Criterion criterion) => criterion switch
    {
        UserCriterion       => typeof(UserCriterion),
        RoleCriterion       => typeof(RoleCriterion),
        PermissionCriterion => typeof(PermissionCriterion),
        ChannelCriterion    => typeof(ChannelCriterion),
        _ => throw new ArgumentOutOfRangeException(nameof(criterion), criterion,
            "Unknown kind of Criterion.")
    };

    private static bool Judge(this IUserEntity auth, IGuildUser user)
        => auth.UserId == user.Id;

    private static bool Judge(this IRoleEntity auth, IGuildUser user)
        => user.RoleIds.Contains(auth.RoleId);

    private static bool Judge(this IPermissionEntity auth, IGuildUser user)
        => (auth.Permission & (GuildPermission) user.GuildPermissions.RawValue) != 0;

    private static bool Judge(this IChannelEntity auth, INestedChannel channel)
        => auth.ChannelId == channel.CategoryId || auth.ChannelId == channel.Id;
}