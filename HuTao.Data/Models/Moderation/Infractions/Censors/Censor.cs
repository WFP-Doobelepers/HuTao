using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Triggers;

namespace HuTao.Data.Models.Moderation.Infractions.Censors;

public class Censor : Trigger, ICensor, ITriggerAction
{
    protected Censor() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public Censor(string pattern, ReprimandAction? action, ICensorOptions? options)
        : base(options)
    {
        Pattern   = pattern;
        Reprimand = action;

        Options = options?.Flags ?? RegexOptions.None;
        Silent  = options?.Silent ?? false;
    }

    [Column(nameof(ITriggerAction.ReprimandId))]
    public Guid? ReprimandId { get; set; }

    public bool Silent { get; set; }

    public virtual ICollection<Criterion> Exclusions { get; set; } = new List<Criterion>();

    public RegexOptions Options { get; set; }

    public string Pattern { get; set; } = null!;

    public virtual ReprimandAction? Reprimand { get; set; }
}