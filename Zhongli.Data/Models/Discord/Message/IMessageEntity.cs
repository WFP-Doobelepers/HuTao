using Discord;

namespace Zhongli.Data.Models.Discord.Message
{
    public interface IMessageEntity : IChannelEntity, IGuildEntity
    {
        ulong MessageId { get; set; }

        string IMentionable.Mention => this.JumpUrl();
    }
}