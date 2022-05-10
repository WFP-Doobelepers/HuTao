using System;
using System.Linq.Expressions;
using Discord;
using HuTao.Data.Models.Discord.Message;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Discord;

public static class EntityExtensions
{
    public static string JumpUrl(this IMessageEntity entity)
        => $"https://discord.com/channels/{entity.GuildId}/{entity.ChannelId}/{entity.MessageId}";

    public static string MentionChannel(this IChannelEntity entity)
        => MentionUtils.MentionChannel(entity.ChannelId);

    public static string MentionRole(this IRoleEntity entity)
        => MentionUtils.MentionRole(entity.RoleId);

    public static string MentionUser(this IUserEntity entity)
        => MentionUtils.MentionUser(entity.UserId);

    public static void AddUserNavigation<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, GuildUserEntity?>> navigationExpression) where T : class, IGuildUserEntity
        => builder
            .HasOne(navigationExpression).WithMany()
            .HasForeignKey(r => new { r.UserId, r.GuildId });
}