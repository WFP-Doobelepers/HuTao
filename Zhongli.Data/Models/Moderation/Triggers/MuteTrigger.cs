using System;

namespace Zhongli.Data.Models.Moderation.Triggers
{
    public class MuteTrigger : WarningTrigger
    {
        public MuteTrigger(uint triggerAt, TimeSpan? length)
            : base(triggerAt)
        {
            Length = length;
        }

        public TimeSpan? Length { get; set; }
    }
}