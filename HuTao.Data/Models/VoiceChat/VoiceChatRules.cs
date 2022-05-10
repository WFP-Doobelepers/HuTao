using System;
using System.Collections.Generic;
using HuTao.Data.Models.Discord;

namespace HuTao.Data.Models.VoiceChat;

public class VoiceChatRules
{
    public Guid Id { get; set; }

    public bool PurgeEmpty { get; set; }

    public bool ShowJoinLeave { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    public virtual ICollection<VoiceChatLink> VoiceChats { get; set; } = new List<VoiceChatLink>();

    public TimeSpan DeletionDelay { get; set; } = TimeSpan.Zero;

    public ulong GuildId { get; set; }

    public ulong HubVoiceChannelId { get; set; }

    public ulong VoiceChannelCategoryId { get; set; }

    public ulong VoiceChatCategoryId { get; set; }
}