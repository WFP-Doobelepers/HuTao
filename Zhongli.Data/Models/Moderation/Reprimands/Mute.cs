using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Mute : ReprimandActionBase
    {
        protected Mute() { }

        public Mute(ReprimandDetails details, DateTimeOffset? startedAt, TimeSpan? length) : base(details)
        {
            StartedAt = startedAt;
            Length    = length;
        }

        public Guid Id { get; set; }

        public bool IsActive => EndedAt is not null || DateTimeOffset.Now >= EndAt;

        public DateTimeOffset? EndAt => StartedAt + Length;

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public TimeSpan? Length { get; set; }

        public TimeSpan? TimeLeft => EndAt - DateTimeOffset.Now;
    }

    public class MuteConfiguration : IEntityTypeConfiguration<Mute>
    {
        public void Configure(EntityTypeBuilder<Mute> builder)
        {
            builder.HasOne(r => r.User)
                .WithMany().HasForeignKey(r => new { r.UserId, r.GuildId });

            builder.HasOne(r => r.Moderator)
                .WithMany().HasForeignKey(r => new { r.ModeratorId, r.GuildId });
        }
    }
}