using HuTao.Data.Models.Discord;

namespace HuTao.Data.Models.Moderation.Logging;

public class ModerationLogChannelConfig : ModerationLogConfig, IChannelEntity
{
    public ulong ChannelId { get; set; }
}