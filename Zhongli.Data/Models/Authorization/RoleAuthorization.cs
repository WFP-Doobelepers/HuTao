namespace Zhongli.Data.Models.Authorization
{
    public class RoleAuthorization : AuthorizationRule
    {
        protected RoleAuthorization() { }

        public RoleAuthorization(ulong roleId) { RoleId = roleId; }

        public ulong RoleId { get; set; }
    }
}