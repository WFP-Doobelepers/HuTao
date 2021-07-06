using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Mute : ReprimandAction, IMute
    {
        protected Mute() { }

        public Mute(ReprimandDetails details, DateTimeOffset? startedAt, TimeSpan? length) : base(details)
        {
            StartedAt = startedAt;
            Length    = length;
        }

        public bool IsActive => EndedAt is not null || DateTimeOffset.Now >= EndAt;

        public DateTimeOffset? EndAt => StartedAt + Length;

        public TimeSpan? TimeLeft => EndAt - DateTimeOffset.Now;

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public TimeSpan? Length { get; set; }
    }
}