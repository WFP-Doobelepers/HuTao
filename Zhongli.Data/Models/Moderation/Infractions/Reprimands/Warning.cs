using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Warning : ExpirableReprimandAction, IWarning
    {
        protected Warning() { }

        public Warning(uint count, TimeSpan? length, ReprimandDetails details) : base(length, details) { Count = count; }

        public uint Count { get; set; }
    }
}