using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Authorization
{
    public interface IAuthorizationRule
    {
        public Guid Id { get; set; }
        
        public ulong GuildId { get; set; }
        
        public GuildEntity Guild { get; set; }

        public AuthorizationScope Scope { get; set; }

        public DateTimeOffset Date { get; set; }

        public GuildUserEntity AddedBy { get; set; }
    }
}