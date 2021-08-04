using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Ban : ExpirableReprimandAction, IBan, IExpirable
    {
        protected Ban() { }

        public Ban(uint deleteDays, TimeSpan? length, ReprimandDetails details) : base(length, details)
        {
            DeleteDays = deleteDays;
        }

        public uint DeleteDays { get; set; }
    }
}