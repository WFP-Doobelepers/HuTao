namespace Zhongli.Data.Models.Authorization
{
    public class PermissionAuthorization : AuthorizationRule
    {
        protected PermissionAuthorization() { }

        public PermissionAuthorization(GuildPermission permission) { Permission = permission; }

        public GuildPermission Permission { get; set; }
    }
}