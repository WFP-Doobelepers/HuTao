using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Infractions.Triggers;

public class ReprimandTrigger : Trigger, ITriggerAction
{
    protected ReprimandTrigger() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ReprimandTrigger(ITrigger? options, TriggerSource source,
        ReprimandAction reprimand) : base(options)
    {
        Source    = source;
        Reprimand = reprimand;
    }

    [Column(nameof(ITriggerAction.ReprimandId))]
    public Guid? ReprimandId { get; set; }

    public TriggerSource Source { get; set; }

    public virtual ReprimandAction? Reprimand { get; set; }
}