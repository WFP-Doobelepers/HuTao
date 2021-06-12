using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class PermissionAuthorization : IAuthorizationRule
    {
        public Guid Id { get; set; }

        public GuildPermission Permission { get; set; }

        public DateTimeOffset Date { get; set; }

        public virtual GuildUserEntity AddedBy { get; set; }

        public AuthorizationScope Scope { get; set; }
    }
}