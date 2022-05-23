using HuTao.Data.Models.Discord;

namespace HuTao.Data.Models.TimeTracking;

public class ChannelTimeTracking : TimeTracking, IChannelEntity
{
    public ulong ChannelId { get; set; }
}