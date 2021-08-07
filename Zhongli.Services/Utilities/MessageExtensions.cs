using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Utilities
{
    public static class MessageExtensions
    {
        private const string Pattern =
            @"(?<Prelink>\S+\s+\S*)?(?<OpenBrace><)?https?://(?:(?:ptb|canary)\.)?discord(app)?\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)/?(?<CloseBrace>>)?(?<Postlink>\S*\s+\S+)?";

        public static readonly Regex JumpUrlRegex =
            new(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
            var match = JumpUrlRegex.Match(jumpUrl);

            if (!ulong.TryParse(match.Groups["GuildId"].Value, out _) ||
                !ulong.TryParse(match.Groups["ChannelId"].Value, out var channelId) ||
                !ulong.TryParse(match.Groups["MessageId"].Value, out var messageId)) return null;

            var channel = await context.Guild.GetTextChannelAsync(channelId);
            return await GetMessageAsync(channel, messageId);
        }

        public static async Task<IMessage?> GetMessageFromUrlAsync(this BaseSocketClient client, string jumpUrl,
            bool allowNsfw = false)
        {
            var match = JumpUrlRegex.Match(jumpUrl);

            if (!ulong.TryParse(match.Groups["GuildId"].Value, out _) ||
                !ulong.TryParse(match.Groups["ChannelId"].Value, out var channelId) ||
                !ulong.TryParse(match.Groups["MessageId"].Value, out var messageId)) return null;

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