namespace Zhongli.Data.Models.Discord
{
    public interface IChannelEntity
    {
        bool IsCategory { get; set; }

        ulong ChannelId { get; set; }
    }
}