using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Moderation.Infractions.Triggers;

public abstract class Trigger : ITrigger, IModerationAction
{
    protected Trigger() { }

    protected Trigger(ITrigger? options)
    {
        Mode   = options?.Mode ?? TriggerMode.Exact;
        Amount = options?.Amount ?? 1;
    }

    public Guid Id { get; set; }

    public bool IsActive { get; set; }

    public virtual ModerationAction? Action { get; set; } = null!;

    public TriggerMode Mode { get; set; }

    public uint Amount { get; set; }
}

public class TriggerConfiguration : IEntityTypeConfiguration<Trigger>
{
    public void Configure(EntityTypeBuilder<Trigger> builder) => builder
        .Property(t => t.IsActive)
        .HasDefaultValue(true);
}