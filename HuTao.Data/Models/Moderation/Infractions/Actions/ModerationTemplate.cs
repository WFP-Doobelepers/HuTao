using System;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class ModerationTemplate
{
    protected ModerationTemplate() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ModerationTemplate(string name, ReprimandAction action, AuthorizationScope scope, string? reason)
    {
        Name   = name;
        Action = action;
        Scope  = scope;
        Reason = reason;
    }

    public Guid Id { get; set; }

    public AuthorizationScope Scope { get; set; }

    public virtual ModerationCategory? Category { get; set; }

    public virtual ReprimandAction Action { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Reason { get; set; }

    public override string? ToString() => Action.ToString();
}