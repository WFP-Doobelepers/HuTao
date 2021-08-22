using Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using static Zhongli.Services.Utilities.DateTimeExtensions;

namespace Zhongli.Services.Moderation
{
    public static class ModerationActionExtensions
    {
        public static string GetDate(this ModerationAction action, TimestampStyle style = TimestampStyle.RelativeTime)
            => action.Date.ToUniversalTimestamp(style);

        public static string GetDate(this IModerationAction action, TimestampStyle style = TimestampStyle.RelativeTime)
            => action.Action?.GetDate(style) ?? "Unknown";

        public static string GetModerator(this ModerationAction action)
            => $"{Format.Bold(action.Mention)} ({action.ModeratorId})";

        public static string GetModerator(this IModerationAction action)
            => action.Action?.GetModerator() ?? "Unknown";

        public static string GetReason(this ModerationAction action)
            => Format.Bold(action.Reason ?? "No reason.");

        public static string GetReason(this IModerationAction action)
            => action.Action?.GetReason() ?? "Unknown";
    }
}