using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Warning : ExpirableReprimandAction
    {
        protected Warning() { }

        public Warning(uint amount, TimeSpan? length, ReprimandDetails details) : base(length, details)
        {
            Amount = amount;
        }

        public uint Amount { get; set; }
    }
}