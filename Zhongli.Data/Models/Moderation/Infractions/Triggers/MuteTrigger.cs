using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class MuteTrigger : Trigger, IMute
    {
        public MuteTrigger(uint amount, TriggerMode mode, TriggerSource source, TimeSpan? length)
            : base(amount, source, mode)
        {
            Length = length;
        }

        public TimeSpan? Length { get; set; }
    }

    public class MuteTriggerConfiguration : IEntityTypeConfiguration<MuteTrigger>
    {
        public void Configure(EntityTypeBuilder<MuteTrigger> builder)
        {
            builder
                .Property(t => t.Length)
                .HasColumnName(nameof(MuteTrigger.Length));
        }
    }
}