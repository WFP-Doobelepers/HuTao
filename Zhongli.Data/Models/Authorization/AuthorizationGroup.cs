using System.Collections.Generic;

namespace Zhongli.Data.Models.Authorization
{
    public class AuthorizationGroup : AuthorizationRule
    {
        public virtual ICollection<AuthorizationRule> Collection { get; set; } = new List<AuthorizationRule>();
    }
}