using Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class PermissionAuthorization : AuthorizationRule
    {
        protected PermissionAuthorization() { }
        
        public PermissionAuthorization(AuthorizationScope scope, IGuildUser moderator, GuildPermission permission) 
            : base(scope, moderator)
        {
            Permission = permission;
        }
        public GuildPermission Permission { get; set; }
    }
}