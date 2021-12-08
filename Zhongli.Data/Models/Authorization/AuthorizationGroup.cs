using System;
using System.Collections.Generic;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Authorization;

public class AuthorizationGroup : IModerationAction
{
    protected AuthorizationGroup() { }

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

    public virtual ModerationAction Action { get; set; }
}