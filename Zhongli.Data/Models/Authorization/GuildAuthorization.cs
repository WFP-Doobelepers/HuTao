using Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class GuildAuthorization : AuthorizationRule
    {
        protected GuildAuthorization() { }
        
        public GuildAuthorization(AuthorizationScope scope, IGuildUser moderator) 
            : base(scope, moderator) { }
    }
}