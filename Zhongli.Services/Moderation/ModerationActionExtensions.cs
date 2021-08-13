using Discord;
using Humanizer;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Services.Moderation
{
    public static class ModerationActionExtensions
    {
        public static string GetDate(this ModerationAction action)
            => $"{Format.Bold(action.Date.Humanize())} ({action.Date.ToUniversalTime()})";

        public static string GetDate(this IModerationAction action)
            => action.Action.GetDate();

        public static string GetModerator(this ModerationAction action)
            => $"{Format.Bold(action.Mention)} ({action.ModeratorId})";

        public static string GetModerator(this IModerationAction action)
            => action.Action.GetModerator();

        public static string GetReason(this ModerationAction action)
            => Format.Bold(action.Reason ?? "No reason.");

        public static string GetReason(this IModerationAction action)
            => action.Action.GetReason();
    }
}