using System;
using Zhongli.Data.Models.Authorization;

namespace Zhongli.Data.Models.Moderation.Infractions.Templates;

public abstract class ModerationTemplate
{
    protected ModerationTemplate() { }

    public ModerationTemplate(TemplateDetails details)
    {
        var (name, reason, scope) = details;

        Name   = name;
        Reason = reason;
        Scope  = scope;
    }

    public Guid Id { get; set; }

    public AuthorizationScope Scope { get; set; }

    public string Name { get; set; }

    public string? Reason { get; set; }

    public override string ToString() => ((IAction) this).Action;
}