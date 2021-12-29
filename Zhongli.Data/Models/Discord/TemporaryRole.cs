using System;
using Discord;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Discord;

public class TemporaryRole : IRoleEntity, IExpirable, IModerationAction
{
    protected TemporaryRole() { }

    public TemporaryRole(IRole role, TimeSpan length)
    {
        RoleId  = role.Id;
        GuildId = role.Guild.Id;

        Length    = length;
        StartedAt = DateTimeOffset.UtcNow;
        ExpireAt  = StartedAt + Length;
    }

    public Guid Id { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset? ExpireAt { get; set; }

    public ulong GuildId { get; set; }

    public TimeSpan? Length { get; set; }

    public virtual ModerationAction? Action { get; set; } = null!;

    public ulong RoleId { get; set; }
}