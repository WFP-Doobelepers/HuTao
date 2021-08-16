using System;
using Zhongli.Data.Models.Moderation.Infractions.Censors;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Censored : ExpirableReprimand
    {
        protected Censored() { }

        public Censored(Censor censor, string content, TimeSpan? length, ReprimandDetails details)
            : base(length, details)
        {
            Censor  = censor;
            Content = content;
        }

        public Guid CensorId { get; set; }

        public virtual Censor Censor { get; set; }

        public string Content { get; set; }
    }
}