using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class MuteTrigger : WarningTrigger, IMute
    {
        public MuteTrigger(uint amount, bool retroactive, TimeSpan? length)
            : base(amount, retroactive)
        {
            Length = length;
        }

        public TimeSpan? Length { get; set; }
    }
}