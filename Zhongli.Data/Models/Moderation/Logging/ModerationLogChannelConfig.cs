using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Logging;

public class ModerationLogChannelConfig : ModerationLogConfig, IChannelEntity
{
    public ulong ChannelId { get; set; }
}