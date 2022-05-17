using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class HardMuteAction : MuteAction, IHardMute
{
    public HardMuteAction(TimeSpan? length) : base(length) { }
}

public class HardMuteConfiguration : IEntityTypeConfiguration<HardMuteAction>
{
    public void Configure(EntityTypeBuilder<HardMuteAction> builder) => builder
        .Property(t => t.Length)
        .HasColumnName(nameof(HardMuteAction.Length));
}