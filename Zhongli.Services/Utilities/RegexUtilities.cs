using System.Text.RegularExpressions;

namespace Zhongli.Services.Utilities
{
    public class RegexUtilities
    {
        private const string JumpLinkPattern
            = @"(?<Prelink>\S+\s+\S*)?(?<OpenBrace><)?https?://(?:(?:ptb|canary)\.)?discord(app)?\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)/?(?<CloseBrace>>)?(?<Postlink>\S*\s+\S+)?";

        private const string DiscordInvitePattern
            = @"discord(?:\.com|app\.com|\.gg)[\/invite\/]?(?:[a-zA-Z0-9\-]{2,32})";

        public static Regex DiscordInvite { get; }
            = new(DiscordInvitePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Regex JumpUrl { get; }
            = new(JumpLinkPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}