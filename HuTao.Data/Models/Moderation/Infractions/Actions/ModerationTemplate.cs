using System;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Authorization;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public record TemplateDetails(string Name, ReprimandAction Action, string? Reason = null,
    AuthorizationScope Scope = AuthorizationScope.Moderator);

public class ModerationTemplate
{
    protected ModerationTemplate() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ModerationTemplate(TemplateDetails details)
    {
        var (name, action, reason, scope) = details;

        Name   = name;
        Action = action;
        Reason = reason;
        Scope  = scope;
    }

    public Guid Id { get; set; }

    public AuthorizationScope Scope { get; set; }

    public virtual ReprimandAction Action { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Reason { get; set; }

    public override string? ToString() => Action.ToString();
}