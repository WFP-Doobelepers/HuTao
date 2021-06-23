using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.VoiceChat
{
    [Owned]
    public class VoiceChatLink
    {
        public Guid Id { get; set; }

        public bool PurgeEmpty { get; set; }

        public bool ShowJoinLeave { get; set; }
        
        public ulong OwnerId { get; set; }

        public ulong GuildId { get; set; }

        public ulong TextChannelId { get; set; }

        public ulong VoiceChannelId { get; set; }
    }

    [Owned]
    public class VoiceChatRules
    {
        public Guid Id { get; set; }
        
        public ulong GuildId { get; set; }

        public ICollection<VoiceChatLink> VoiceChats { get; set; } = new List<VoiceChatLink>();
    }
}