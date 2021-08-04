using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Notice : ExpirableReprimandAction
    {
        protected Notice() { }

        public Notice(TimeSpan? length, ReprimandDetails details) : base(length, details) { }
    }
}