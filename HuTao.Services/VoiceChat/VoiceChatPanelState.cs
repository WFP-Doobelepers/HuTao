using System;
using Discord;

namespace HuTao.Services.VoiceChat;

public sealed class VoiceChatPanelState
{
    public required string GuildName { get; init; }
    public required ulong VoiceChannelId { get; init; }
    public required ulong TextChannelId { get; init; }
    public required ulong OwnerUserId { get; set; }

    public bool IsLocked { get; set; }
    public bool IsHidden { get; set; }
    public int? UserLimit { get; set; }

    public DateTimeOffset? LastUpdated { get; set; }
    public string? Notice { get; set; }

    public string VoiceChannelMention => MentionUtils.MentionChannel(VoiceChannelId);
    public string TextChannelMention => MentionUtils.MentionChannel(TextChannelId);
    public string OwnerMention => MentionUtils.MentionUser(OwnerUserId);

    public static VoiceChatPanelState Create(string guildName, ulong voiceChannelId, ulong textChannelId, ulong ownerUserId)
        => new()
        {
            GuildName = guildName,
            VoiceChannelId = voiceChannelId,
            TextChannelId = textChannelId,
            OwnerUserId = ownerUserId,
            LastUpdated = DateTimeOffset.UtcNow
        };

    public void UpdateStatus(bool locked, bool hidden, int? userLimit)
    {
        IsLocked = locked;
        IsHidden = hidden;
        UserLimit = userLimit;
        LastUpdated = DateTimeOffset.UtcNow;
    }
}

