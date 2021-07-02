namespace Zhongli.Data.Models.Authorization
{
    public class PermissionAuthorization : AuthorizationRule
    {
        public GuildPermission Permission { get; set; }
    }
}