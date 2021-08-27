using Discord;
using Zhongli.Data.Models.Discord.Message;

namespace Zhongli.Data.Models.Discord
{
    public static class EntityExtensions
    {
        public static string JumpUrl(this IMessageEntity entity)
            => $"https://discord.com/channels/{entity.GuildId}/{entity.ChannelId}/{entity.MessageId}";

        public static string MentionChannel(this IChannelEntity entity)
            => $"<#{entity.ChannelId}>";

        public static string MentionRole(this IRoleEntity entity)
            => $"<@&{entity.RoleId}>";

        public static string MentionUser(this IUserEntity entity)
            => $"<@{entity.UserId}>";

        public static Thumbnail ToThumbnail(this EmbedThumbnail thumbnail)
            => new(thumbnail);
    }
}