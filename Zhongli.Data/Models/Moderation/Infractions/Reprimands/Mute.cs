using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Mute : ReprimandAction, IMute, IExpire
    {
        protected Mute() { }

        public Mute(DateTimeOffset? startedAt, TimeSpan? length, ReprimandDetails details) : base(details)
        {
            StartedAt = startedAt;
            Length    = length;
        }

        public bool IsActive => EndedAt is null || EndAt >= DateTimeOffset.Now;

        public DateTimeOffset? EndAt => StartedAt + Length;

        public TimeSpan? TimeLeft => EndAt - DateTimeOffset.Now;

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public TimeSpan? Length { get; set; }
    }
}