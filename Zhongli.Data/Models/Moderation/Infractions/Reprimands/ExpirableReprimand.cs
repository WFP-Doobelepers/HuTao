using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public abstract class ExpirableReprimand : Reprimand, IExpirable
    {
        protected ExpirableReprimand() { }

        protected ExpirableReprimand(TimeSpan? length, ReprimandDetails details) : base(details)
        {
            Length    = length;
            StartedAt = DateTimeOffset.UtcNow;
            ExpireAt  = StartedAt + Length;
        }

        public DateTimeOffset StartedAt { get; set; }

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset? ExpireAt { get; set; }

        public TimeSpan? Length { get; set; }
    }

    public class ExpireReprimandActionConfiguration : IEntityTypeConfiguration<ExpirableReprimand>
    {
        public void Configure(EntityTypeBuilder<ExpirableReprimand> builder)
        {
            builder
                .Property(r => r.EndedAt)
                .HasColumnName(nameof(ExpirableReprimand.EndedAt));

            builder
                .Property(r => r.StartedAt)
                .HasColumnName(nameof(ExpirableReprimand.StartedAt));

            builder
                .Property(r => r.Length)
                .HasColumnName(nameof(ExpirableReprimand.Length));
        }
    }
}