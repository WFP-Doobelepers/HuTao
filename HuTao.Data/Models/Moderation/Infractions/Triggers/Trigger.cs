using System;
using System.Diagnostics.CodeAnalysis;

namespace HuTao.Data.Models.Moderation.Infractions.Triggers;

public abstract class Trigger : ITrigger, IModerationAction
{
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected Trigger(ITrigger? options = null)
    {
        Category = options?.Category?.Id == Guid.Empty ? null : options?.Category;
        Mode     = options?.Mode ?? TriggerMode.Exact;
        Amount   = options?.Amount ?? 1;
        IsActive = true;
    }

    public Guid Id { get; set; }

    public bool IsActive { get; set; }

    public virtual ModerationAction? Action { get; set; }

    public virtual ModerationCategory? Category { get; set; }

    public TriggerMode Mode { get; set; }

    public uint Amount { get; set; }
}