using System;
using System.Collections.Generic;

namespace Zhongli.Data.Models.Authorization
{
    public class AuthorizationRules
    {
        public Guid Id { get; set; }

        public virtual ICollection<ChannelAuthorization> ChannelAuthorizations { get; init; } =
            new List<ChannelAuthorization>();

        public virtual ICollection<GuildAuthorization> GuildAuthorizations { get; init; } =
            new List<GuildAuthorization>();

        public virtual ICollection<PermissionAuthorization> PermissionAuthorizations { get; init; } =
            new List<PermissionAuthorization>();

        public virtual ICollection<RoleAuthorization> RoleAuthorizations { get; init; } = new List<RoleAuthorization>();

        public virtual ICollection<UserAuthorization> UserAuthorizations { get; init; } = new List<UserAuthorization>();
    }
}