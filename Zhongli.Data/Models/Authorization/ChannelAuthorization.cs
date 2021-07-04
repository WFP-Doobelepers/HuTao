using System;
using Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class ChannelAuthorization : AuthorizationRule
    {
        protected ChannelAuthorization() { }
        
        public ChannelAuthorization(AuthorizationScope scope, IGuildUser moderator, ulong channelId, bool isCategory)
            : base(scope, moderator)
        {
            ChannelId  = channelId;
            IsCategory = isCategory;
        }
        
        public ulong ChannelId { get; set; }
        
        public bool IsCategory { get; set; }
    }
}