using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class MuteTrigger : WarningTrigger, IMute
    {
        public MuteTrigger(uint amount, TimeSpan? length)
            : base(amount)
        {
            Length = length;
        }

        public TimeSpan? Length { get; set; }
    }
}