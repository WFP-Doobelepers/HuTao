using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord.Message;

namespace Zhongli.Data.Models.Discord;

public static class EntityExtensions
{
    public static string JumpUrl(this IMessageEntity entity)
        => $"https://discord.com/channels/{entity.GuildId}/{entity.ChannelId}/{entity.MessageId}";

    public static string MentionChannel(this IChannelEntity entity)
        => $"<#{entity.ChannelId}>";

    public static string MentionRole(this IRoleEntity entity)
        => $"<@&{entity.RoleId}>";

    public static string MentionUser(this IUserEntity entity)
        => $"<@{entity.UserId}>";

    public static void AddUserNavigation<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, GuildUserEntity?>> navigationExpression) where T : class, IGuildUserEntity
        => builder
            .HasOne(navigationExpression).WithMany()
            .HasForeignKey(r => new { r.UserId, r.GuildId });
}