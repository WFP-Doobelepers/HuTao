using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Mute : ReprimandActionBase
    {
        protected Mute() { }

        public Guid Id { get; set; }

        public Mute(ReprimandDetails details, DateTimeOffset? startedAt, TimeSpan? length) : base(details)
        {
            StartedAt = startedAt;
            Length    = length;
        }

        public bool IsActive => EndedAt is not null || DateTimeOffset.Now >= EndAt;

        public DateTimeOffset? EndAt => StartedAt + Length;

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public TimeSpan? Length { get; set; }

        public TimeSpan? TimeLeft => EndAt - DateTimeOffset.Now;
    }
}