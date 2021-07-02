namespace Zhongli.Data.Models.Authorization
{
    public class RoleAuthorization : AuthorizationRule
    {
        public ulong RoleId { get; set; }
    }
}