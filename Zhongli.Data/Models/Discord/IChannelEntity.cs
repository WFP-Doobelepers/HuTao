namespace Zhongli.Data.Models.Discord;

public interface IGuildChannelEntity : IChannelEntity
{
    bool IsCategory { get; set; }
}

public interface IChannelEntity
{
    ulong ChannelId { get; set; }
}