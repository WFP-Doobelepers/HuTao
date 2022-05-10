namespace HuTao.Data.Models.TimeTracking;

public class MessageTimeTracking : TimeTracking
{
    public ulong ChannelId { get; set; }

    public ulong MessageId { get; set; }
}