using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Utilities
{
    public static class MessageExtensions
    {
        public static bool TryGetJumpUrl(Match match, out ulong guildId, out ulong channelId, out ulong messageId) =>
            ulong.TryParse(match.Groups["GuildId"].Value, out guildId)
            & ulong.TryParse(match.Groups["ChannelId"].Value, out channelId)
            & ulong.TryParse(match.Groups["MessageId"].Value, out messageId);

        public static string GetJumpUrlForEmbed(this IMessage message)
            => Format.Url($"#{message.Channel.Name} (click here)", message.GetJumpUrl());

        public static async Task<IMessage?> GetMessageAsync(this BaseSocketClient client, ulong channelId,
            ulong messageId, bool allowNsfw = false)
        {
            if (client.GetChannel(channelId) is not ITextChannel channel)
                return null;

            return await GetMessageAsync(channel, messageId);
        }

        public static async Task<IMessage?> GetMessageAsync(this ICommandContext context,
            ulong messageId, bool allowNsfw = false)
        {
            if (context.Channel is not ITextChannel textChannel)
                return null;

            if (textChannel.IsNsfw && !allowNsfw)
                return null;

            return await GetMessageAsync(textChannel, messageId);
        }

        public static async Task<IMessage?> GetMessageFromUrlAsync(this ICommandContext context, string jumpUrl,
            bool allowNsfw = false)
        {
            if (!TryGetJumpUrl(RegexUtilities.JumpUrl.Match(jumpUrl), out _, out var channelId, out var messageId))
                return null;

            var channel = await context.Guild.GetTextChannelAsync(channelId);
            return await GetMessageAsync(channel, messageId);
        }

        public static async Task<IMessage?> GetMessageFromUrlAsync(this BaseSocketClient client, string jumpUrl,
            bool allowNsfw = false)
        {
            if (!TryGetJumpUrl(RegexUtilities.JumpUrl.Match(jumpUrl), out _, out var channelId, out var messageId))
                return null;

            return await client.GetMessageAsync(channelId, messageId, allowNsfw);
        }

        private static async Task<IMessage?> GetMessageAsync(this ITextChannel channel, ulong messageId,
            bool allowHidden = false, bool allowNsfw = false)
        {
            if (channel.IsNsfw && !allowNsfw)
                return null;

            var currentUser = await channel.Guild.GetCurrentUserAsync();
            var channelPermissions = currentUser.GetPermissions(channel);

            if (!channelPermissions.ViewChannel && !allowHidden)
                return null;

            var cacheMode = channelPermissions.ReadMessageHistory
                ? CacheMode.AllowDownload
                : CacheMode.CacheOnly;

            return await channel.GetMessageAsync(messageId, cacheMode);
        }
    }
}