using System;
using System.Collections.Generic;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Authorization
{
    public class AuthorizationGroup : IModerationAction
    {
        protected AuthorizationGroup() { }

        public AuthorizationGroup(AuthorizationScope scope, ICollection<Criterion> rules)
        {
            Scope      = scope;
            Collection = rules;
        }

        public Guid Id { get; set; }

        public AuthorizationScope Scope { get; set; }

        public virtual ICollection<Criterion> Collection { get; set; } = new List<Criterion>();

        public ModerationAction Action { get; set; }
    }
}