using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Zhongli.Data.Models.Criteria;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public abstract class Censor : IModerationAction, ICensor
    {
        protected Censor() { }

        protected Censor(string pattern, RegexOptions options = RegexOptions.None)
        {
            Pattern = pattern;
            Options = options;
        }

        public Guid Id { get; set; }

        public virtual ICollection<Criterion> Exclusions { get; set; }

        public RegexOptions Options { get; set; }

        public string Pattern { get; set; }

        public virtual ModerationAction Action { get; set; }
    }
}