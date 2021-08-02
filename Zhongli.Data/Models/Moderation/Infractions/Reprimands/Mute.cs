using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Mute : ReprimandAction, IMute, IExpire
    {
        protected Mute() { }

        public Mute(TimeSpan? length, ReprimandDetails details) : base(details)
        {
            StartedAt = DateTimeOffset.Now;
            Length    = length;
        }

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset StartedAt { get; set; }

        public TimeSpan? Length { get; set; }
    }

    public class MuteConfiguration : IEntityTypeConfiguration<Mute>
    {
        public void Configure(EntityTypeBuilder<Mute> builder)
        {
            builder
                .Property(r => r.EndedAt)
                .HasColumnName(nameof(Mute.EndedAt));

            builder
                .Property(r => r.StartedAt)
                .HasColumnName(nameof(Mute.StartedAt));

            builder
                .Property(r => r.Length)
                .HasColumnName(nameof(Mute.Length));
        }
    }
}