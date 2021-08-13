using Discord;

namespace Zhongli.Data.Models.Discord
{
    public interface IChannelEntity : IMentionable
    {
        bool IsCategory { get; set; }

        ulong ChannelId { get; set; }

        string IMentionable.Mention => $"<#{ChannelId}>";
    }
}