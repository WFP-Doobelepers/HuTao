using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Quote;

namespace Zhongli.Services.Utilities;

public static class MessageExtensions
{
    public static bool IsJumpUrls(string text) => string.IsNullOrWhiteSpace(CleanJumpUrls(text));

    public static IEnumerable<JumpMessage> GetJumpMessages(string text)
        => RegexUtilities.JumpUrl.Matches(text).Select(ToJumpMessage).OfType<JumpMessage>();

    public static JumpMessage? GetJumpMessage(string text)
        => ToJumpMessage(RegexUtilities.JumpUrl.Match(text));

    public static string GetJumpUrl(this MessageLog message)
        => $"https://discord.com/channels/{message.GuildId}/{message.ChannelId}/{message.MessageId}";

    public static string GetJumpUrlForEmbed(this IMessage message)
        => Format.Url($"#{message.Channel.Name} (Jump)", message.GetJumpUrl());

    public static string GetJumpUrlForEmbed(this MessageLog message)
        => Format.Url($"{message.MentionChannel()} (Jump)", message.GetJumpUrl());

    public static async Task<IMessage?> GetMessageAsync(
        this JumpMessage jump, Context context,
        bool allowHidden = false, bool? allowNsfw = null)
    {
        var (guildId, channelId, messageId, ignored) = jump;
        if (ignored || context.Guild.Id != guildId) return null;

        var channel = await context.Guild.GetTextChannelAsync(channelId);
        if (channel is null) return null;

        return await GetMessageAsync(channel, messageId, allowHidden, allowNsfw ?? channel.IsNsfw);
    }

    private static JumpMessage? ToJumpMessage(Match match)
        => ulong.TryParse(match.Groups["GuildId"].Value, out var guildId)
            && ulong.TryParse(match.Groups["ChannelId"].Value, out var channelId)
            && ulong.TryParse(match.Groups["MessageId"].Value, out var messageId)
                ? new JumpMessage(guildId, channelId, messageId,
                    match.Groups["OpenBrace"].Success && match.Groups["CloseBrace"].Success)
                : null;

    private static string CleanJumpUrls(string text) => RegexUtilities.JumpUrl.Replace(text, m
        => m.Groups["OpenBrace"].Success && m.Groups["CloseBrace"].Success ? m.Value : string.Empty);

    private static async Task<IMessage?> GetMessageAsync(
        this ITextChannel channel, ulong messageId,
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