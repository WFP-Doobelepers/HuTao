using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Services.Quote;

namespace HuTao.Services.Utilities;

public static class MessageExtensions
{
    public static bool IsDuplicate(this DuplicateConfiguration config, IMessage first, IMessage second)
        => config.IsDuplicate(first.Content, second.Content);

    public static bool IsDuplicate(this DuplicateConfiguration config, string first, string second)
        => first.Memoized(second, s => first.LevenshteinDistance(s) <= config.Tolerance);

    public static bool IsJumpUrls(string text) => string.IsNullOrWhiteSpace(CleanJumpUrls(text));

    public static IEnumerable<JumpMessage> GetJumpMessages(string text)
        => RegexUtilities.JumpUrl.Matches(text).Select(ToJumpMessage).OfType<JumpMessage>();

    public static int LevenshteinDistance(this IMessage first, IMessage second)
        => first.Content.Memoized(second.Content, c => first.Content.LevenshteinDistance(c));

    public static int LevenshteinDistance(this string first, string second)
    {
        var n = first.Length;
        var m = second.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; d[i, 0] = i++) { }
        for (var j = 0; j <= m; d[0, j] = j++) { }

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = second[j - 1] == first[i - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    public static JumpMessage? GetJumpMessage(string text)
        => ToJumpMessage(RegexUtilities.JumpUrl.Match(text));

    public static string GetJumpUrl(this MessageLog message)
        => $"https://discord.com/channels/{message.GuildId}/{message.ChannelId}/{message.MessageId}";

    public static string GetJumpUrlForEmbed(this IMessage message)
        => Format.Url($"#{message.Channel.Name} (Jump)", message.GetJumpUrl());

    public static string GetJumpUrlForEmbed(this MessageLog message)
        => Format.Url($"{message.MentionChannel()} (Jump)", message.GetJumpUrl());

    public static Task<IMessage?> GetMessageAsync(
        this QuotedMessage jump,
        bool allowHidden = false, bool? allowNsfw = null)
        => jump.GetMessageAsync(jump.Context, allowHidden, allowNsfw);

    public static async Task<IMessage?> GetMessageAsync(
        this JumpMessage jump, Context context,
        bool allowHidden = false, bool? allowNsfw = null)
    {
        if (context.User is not IGuildUser user) return null;

        var (guildId, channelId, messageId, ignored) = jump;
        if (ignored || context.Guild.Id != guildId) return null;

        var channel = await context.Guild.GetTextChannelAsync(channelId);
        if (channel is null) return null;

        allowNsfw ??= (context.Channel as ITextChannel)?.IsNsfw;
        return await GetMessageAsync(channel, messageId, user, allowHidden, allowNsfw ?? false);
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
        this ITextChannel channel, ulong messageId, IGuildUser user,
        bool allowHidden = false, bool allowNsfw = false)
    {
        if (channel.IsNsfw && !allowNsfw)
            return null;

        var channelPermissions = user.GetPermissions(channel);
        if (!channelPermissions.ViewChannel && !allowHidden)
            return null;

        var cacheMode = channelPermissions.ReadMessageHistory
            ? CacheMode.AllowDownload
            : CacheMode.CacheOnly;

        return await channel.GetMessageAsync(messageId, cacheMode);
    }
}