using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Moderation.Infractions.Actions;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers;

public class ReprimandTrigger : Trigger, ITriggerAction
{
    protected ReprimandTrigger() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ReprimandTrigger(ITrigger? options, TriggerSource source,
        ReprimandAction reprimand) : base(options)
    {
        Source    = source;
        Reprimand = reprimand;
    }

    public Guid? ReprimandId { get; set; }

    public TriggerSource Source { get; set; }

    public virtual ReprimandAction? Reprimand { get; set; }
}

public class ReprimandTriggerConfiguration : IEntityTypeConfiguration<ReprimandTrigger>
{
    public void Configure(EntityTypeBuilder<ReprimandTrigger> builder) => builder
        .Property(t => t.ReprimandId)
        .HasColumnName(nameof(ITriggerAction.ReprimandId));
}