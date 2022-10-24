using System;
using System.ComponentModel.DataAnnotations.Schema;
using Discord;
using HuTao.Data.Models.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Discord;

public class GuildUserEntity
{
    protected GuildUserEntity() { }

    public GuildUserEntity(IGuildUser user) : this(user, user.Guild) { JoinedAt = user.JoinedAt?.ToUniversalTime(); }

    public GuildUserEntity(IUser user, IGuild guild) : this(user.Id, guild.Id) { }

    public GuildUserEntity(ulong id, ulong guild)
    {
        Id      = id;
        GuildId = guild;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public DateTimeOffset? JoinedAt { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    public virtual ModerationCategory? DefaultCategory { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong GuildId { get; set; }
}

public class GuildUserEntityConfiguration : IEntityTypeConfiguration<GuildUserEntity>
{
    public void Configure(EntityTypeBuilder<GuildUserEntity> builder) => builder.HasKey(w => new { w.Id, w.GuildId });
}