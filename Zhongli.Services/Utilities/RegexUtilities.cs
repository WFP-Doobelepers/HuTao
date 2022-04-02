using System.Text.RegularExpressions;

namespace Zhongli.Services.Utilities;

public class RegexUtilities
{
    public static Regex DiscordInvite { get; }
        = new(@"discord(?:\.com|app\.com|\.gg)[\/invite\/]?(?:[a-zA-Z0-9\-]{2,32})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static Regex JumpUrl { get; } = new(@"
            (?<OpenBrace><)?
            https?://((www|ptb|canary)\.)?discord(app)?\.com/channels/
                (?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)/?
            (?<CloseBrace>>)?",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture |
        RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
}