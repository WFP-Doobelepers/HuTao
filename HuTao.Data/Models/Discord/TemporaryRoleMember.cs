using System;
using Discord;
using HuTao.Data.Models.Moderation.Infractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Discord;

public class TemporaryRoleMember : IRoleEntity, IGuildUserEntity, IExpirable, IModerationAction
{
    protected TemporaryRoleMember() { }

    public TemporaryRoleMember(IGuildUser user, IRole role, TimeSpan length)
    {
        UserId  = user.Id;
        RoleId  = role.Id;
        GuildId = role.Guild.Id;

        Length    = length;
        StartedAt = DateTimeOffset.UtcNow;
        ExpireAt  = StartedAt + Length;
    }

    public virtual GuildUserEntity User { get; set; } = null!;

    public Guid Id { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset? ExpireAt { get; set; }

    public TimeSpan? Length { get; set; }

    public ulong UserId { get; set; }

    public virtual ModerationAction? Action { get; set; }

    public ulong GuildId { get; set; }

    public ulong RoleId { get; set; }
}

public class TemporaryRoleMemberConfiguration : IEntityTypeConfiguration<TemporaryRoleMember>
{
    public void Configure(EntityTypeBuilder<TemporaryRoleMember> builder) => builder.AddUserNavigation(r => r.User);
}