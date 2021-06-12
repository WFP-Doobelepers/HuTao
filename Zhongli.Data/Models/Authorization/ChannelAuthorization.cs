using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class ChannelAuthorization : IAuthorizationRule
    {
        public Guid Id { get; set; }

        public ulong ChannelId { get; set; }

        public AuthorizationScope Scope { get; set; }

        public DateTimeOffset Date { get; set; }

        public virtual GuildUserEntity AddedBy { get; set; }
    }
}