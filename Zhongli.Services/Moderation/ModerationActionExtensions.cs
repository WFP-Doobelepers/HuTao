using Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation
{
    public static class ModerationActionExtensions
    {
        public static string GetDate(this ModerationAction action)
            => action.Date.ToUniversalTimestamp();

        public static string GetDate(this IModerationAction action)
            => action.Action?.GetDate() ?? "Unknown";

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