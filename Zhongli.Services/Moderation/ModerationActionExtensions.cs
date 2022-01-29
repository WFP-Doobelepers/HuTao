using Discord;
using Humanizer;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation;

public static class ModerationActionExtensions
{
    public static EmbedBuilder WithTimestamp(this EmbedBuilder builder, IModerationAction action, bool useFooter = true)
        => builder.WithTimestamp(action.Action, useFooter);

    public static string GetDate(this ModerationAction action)
        => action.Date.ToUniversalTimestamp();

    public static string GetDate(this IModerationAction action)
        => action.Action?.GetDate() ?? "Unknown";

    public static string GetModerator(this ModerationAction action)
        => $"{Format.Bold(action.MentionUser())} ({action.UserId})";

    public static string GetModerator(this IModerationAction action)
        => action.Action?.GetModerator() ?? "Unknown";

    public static string GetReason(this ModerationAction action, int length = 256)
        => Format.Bold(action.Reason?.Length > length
            ? $"{action.Reason.Truncate(length)}"
            : action.Reason ?? "No reason.");

    public static string GetReason(this IModerationAction action, int length = 256)
        => action.Action?.GetReason(length) ?? "Unknown";

    private static EmbedBuilder WithTimestamp(this EmbedBuilder builder, ModerationAction? action,
        bool useFooter = true)
    {
        if (action is not null) builder.WithTimestamp(action.Date);
        return useFooter ? builder.WithFooter(action?.Date.Humanize().Humanize(LetterCasing.Sentence)) : builder;
    }
}