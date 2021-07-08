namespace Zhongli.Data.Models.Discord
{
    public interface IChannelEntity
    {
        ulong ChannelId { get; set; }

        bool IsCategory { get; set; }
    }
}