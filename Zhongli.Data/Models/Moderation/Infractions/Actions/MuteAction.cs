using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Actions;

public class MuteAction : ReprimandAction, IMute
{
    public MuteAction(TimeSpan? length) { Length = length; }

    public TimeSpan? Length { get; set; }
}

public class MuteActionConfiguration : IEntityTypeConfiguration<MuteAction>
{
    public void Configure(EntityTypeBuilder<MuteAction> builder) => builder
        .Property(t => t.Length)
        .HasColumnName(nameof(MuteAction.Length));
}