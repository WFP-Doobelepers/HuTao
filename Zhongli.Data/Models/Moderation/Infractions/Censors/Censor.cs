using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class Censor : Trigger, ICensor, ITriggerAction
    {
        protected Censor() { }

        public Censor(string pattern, ReprimandAction? action, ICensorOptions? options)
            : base(options)
        {
            Pattern   = pattern;
            Reprimand = action;

            Options = options?.Flags ?? RegexOptions.None;
            Silent  = options?.Silent ?? false;
        }

        public Guid? ReprimandId { get; set; }

        public bool Silent { get; set; }

        public virtual ICollection<Criterion> Exclusions { get; set; }

        public RegexOptions Options { get; set; }

        public string Pattern { get; set; }

        public virtual ReprimandAction? Reprimand { get; set; }
    }

    public class CensorConfiguration : IEntityTypeConfiguration<Censor>
    {
        public void Configure(EntityTypeBuilder<Censor> builder)
        {
            builder
                .Property(t => t.ReprimandId)
                .HasColumnName(nameof(ITriggerAction.ReprimandId));
        }
    }
}