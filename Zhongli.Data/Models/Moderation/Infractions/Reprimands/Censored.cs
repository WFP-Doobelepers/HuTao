using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Censored : ExpirableReprimand
    {
        protected Censored() { }

        public Censored(string content, TimeSpan? length, ReprimandDetails details)
            : base(length, details)
        {
            Content = content;
        }

        public string Content { get; set; }
    }
}