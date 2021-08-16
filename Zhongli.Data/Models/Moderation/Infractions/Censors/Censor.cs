using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public abstract class Censor : ICensor, ITrigger, IModerationAction
    {
        protected Censor() { }

        protected Censor(string pattern, ICensorOptions? options)
        {
            Pattern = pattern;
            Options = options?.Flags ?? RegexOptions.None;

            Mode   = options?.TriggerMode ?? TriggerMode.Default;
            Amount = options?.TriggerAt ?? 1;
        }

        public Guid Id { get; set; }

        public virtual ICollection<Criterion> Exclusions { get; set; }

        public RegexOptions Options { get; set; }

        public string Pattern { get; set; }

        public virtual ModerationAction Action { get; set; }

        public TriggerMode Mode { get; set; }

        public uint Amount { get; set; }
    }
}