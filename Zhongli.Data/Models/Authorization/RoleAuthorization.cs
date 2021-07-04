using Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class RoleAuthorization : AuthorizationRule
    {
        protected RoleAuthorization() { }

        public RoleAuthorization(AuthorizationScope scope, IGuildUser moderator, ulong roleId)
            : base(scope, moderator)
        {
            RoleId = roleId;
        }

        public ulong RoleId { get; set; }
    }
}