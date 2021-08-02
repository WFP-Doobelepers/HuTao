using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Ban : ReprimandAction, IBan, IExpire
    {
        protected Ban() { }

        public Ban(uint deleteDays, TimeSpan? length, ReprimandDetails details) : base(details)
        {
            StartedAt  = DateTimeOffset.Now;
            DeleteDays = deleteDays;
            Length     = length;
        }

        public uint DeleteDays { get; set; }

        public TimeSpan? Length { get; set; }

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset StartedAt { get; set; }
    }

    public class BanConfiguration : IEntityTypeConfiguration<Ban>
    {
        public void Configure(EntityTypeBuilder<Ban> builder)
        {
            builder
                .Property(r => r.EndedAt)
                .HasColumnName(nameof(Ban.EndedAt));

            builder
                .Property(r => r.StartedAt)
                .HasColumnName(nameof(Ban.StartedAt));

            builder
                .Property(r => r.Length)
                .HasColumnName(nameof(Ban.Length));
        }
    }
}