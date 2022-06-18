using System;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Authorization;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public interface ITemplateOptions
{
    public AuthorizationScope Scope { get; }

    public ModerationCategory? Category { get; }

    public string? Reason { get; }
}

public class ModerationTemplate
{
    protected ModerationTemplate() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ModerationTemplate(string name, ReprimandAction action, ITemplateOptions options)
    {
        Name   = name;
        Action = action;
        Scope  = options.Scope;
        Reason = options.Reason;
    }

    public Guid Id { get; set; }

    public AuthorizationScope Scope { get; set; }

    public virtual ModerationCategory? Category { get; set; }

    public virtual ReprimandAction Action { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Reason { get; set; }

    public override string? ToString() => Action.ToString();
}