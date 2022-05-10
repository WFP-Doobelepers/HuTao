using System;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public abstract class ReprimandAction
{
    public Guid Id { get; set; }

    public override string? ToString() => (this as IAction)?.Action;
}