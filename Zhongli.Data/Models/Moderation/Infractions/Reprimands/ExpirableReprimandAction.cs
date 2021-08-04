using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public abstract class ExpirableReprimandAction : ReprimandAction, IExpirable
    {
        protected ExpirableReprimandAction() { }

        protected ExpirableReprimandAction(TimeSpan? length, ReprimandDetails details) : base(details)
        {
            StartedAt = DateTimeOffset.Now;
            Length    = length;
            ExpireAt  = StartedAt + Length;
        }

        public TimeSpan? Length { get; set; }

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset? ExpireAt { get; set; }

        public DateTimeOffset StartedAt { get; set; }
    }

    public class ExpireReprimandActionConfiguration : IEntityTypeConfiguration<ExpirableReprimandAction>
    {
        public void Configure(EntityTypeBuilder<ExpirableReprimandAction> builder)
        {
            builder
                .Property(r => r.EndedAt)
                .HasColumnName(nameof(ExpirableReprimandAction.EndedAt));

            builder
                .Property(r => r.StartedAt)
                .HasColumnName(nameof(ExpirableReprimandAction.StartedAt));

            builder
                .Property(r => r.Length)
                .HasColumnName(nameof(ExpirableReprimandAction.Length));
        }
    }
}