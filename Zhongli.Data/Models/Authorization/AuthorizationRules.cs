using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class AuthorizationRules
    {
        public Guid Id { get; set; }

        public ulong GuildId { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual ICollection<ChannelAuthorization> ChannelAuthorizations { get; set; }
            = new List<ChannelAuthorization>();

        public virtual ICollection<GuildAuthorization> GuildAuthorizations { get; set; }
            = new List<GuildAuthorization>();

        public virtual ICollection<PermissionAuthorization> PermissionAuthorizations { get; set; }
            = new List<PermissionAuthorization>();

        public virtual ICollection<RoleAuthorization> RoleAuthorizations { get; set; }
            = new List<RoleAuthorization>();

        public virtual ICollection<UserAuthorization> UserAuthorizations { get; set; }
            = new List<UserAuthorization>();
        
        public virtual ICollection<AuthorizationGroup> AuthorizationGroups { get; set; }
            = new List<AuthorizationGroup>();
    }
}