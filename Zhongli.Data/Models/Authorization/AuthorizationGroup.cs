using System;
using System.Collections.Generic;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Authorization
{
    public class AuthorizationGroup : IModerationAction
    {
        protected AuthorizationGroup() { }

        public AuthorizationGroup(AuthorizationScope scope, ICollection<AuthorizationRule> rules)
        {
            Scope      = scope;
            Collection = rules;
        }

        public Guid Id { get; set; }

        public AuthorizationScope Scope { get; set; }

        public virtual ICollection<AuthorizationRule> Collection { get; set; } = new List<AuthorizationRule>();

        public ModerationAction Action { get; set; }
    }
}