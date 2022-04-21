using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Infractions;

namespace HuTao.Data.Models.Authorization;

public class AuthorizationGroup : IModerationAction
{
    protected AuthorizationGroup() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public AuthorizationGroup(AuthorizationScope scope, AccessType access, ICollection<Criterion> rules)
    {
        Scope      = scope;
        Access     = access;
        Collection = rules;
    }

    public Guid Id { get; set; }

    public AccessType Access { get; set; }

    public AuthorizationScope Scope { get; set; }

    public virtual ICollection<Criterion> Collection { get; set; } = new List<Criterion>();

    public virtual ModerationAction? Action { get; set; } = null!;
}