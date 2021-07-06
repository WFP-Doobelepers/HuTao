namespace Zhongli.Data.Models.Authorization
{
    public class UserAuthorization : AuthorizationRule
    {
        protected UserAuthorization() { }

        public UserAuthorization(ulong userId) { UserId = userId; }

        public ulong UserId { get; set; }
    }
}