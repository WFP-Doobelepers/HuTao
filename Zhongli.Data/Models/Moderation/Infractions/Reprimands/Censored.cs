using System;
using System.Text.RegularExpressions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Censored : ReprimandAction, ICensor
    {
        protected Censored() { }

        public Censored(Censor censor, string content, ReprimandDetails details) : base(details)
        {
            Censor = censor;

            Pattern = censor.Pattern;
            Options = censor.Options;

            Content = content;
        }

        public Guid CensorId { get; set; }

        public virtual Censor Censor { get; set; }

        public string Content { get; set; }

        public RegexOptions Options { get; set; }

        public string Pattern { get; set; }
    }
}