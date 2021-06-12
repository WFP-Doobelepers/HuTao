using System;
using System.Collections.Generic;

namespace Zhongli.Data.Models.Authorization
{
    public class AuthorizationRules
    {
        public Guid Id { get; set; }

        public virtual ICollection<ChannelAuthorization> ChannelAuthorizations { get; set; }

        public virtual ICollection<GuildAuthorization> ServerAuthorizations { get; set; }

        public virtual ICollection<PermissionAuthorization> PermissionAuthorizations { get; set; }

        public virtual ICollection<RoleAuthorization> RoleAuthorizations { get; set; }

        public virtual ICollection<UserAuthorization> UserAuthorizations { get; set; }
    }
}