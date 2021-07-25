namespace Zhongli.Data.Models.TimeTracking
{
    public class MessageTimeTracking : TimeTracking
    {
        public ulong MessageId { get; set; }

        public ulong ChannelId { get; set; }
    }
}