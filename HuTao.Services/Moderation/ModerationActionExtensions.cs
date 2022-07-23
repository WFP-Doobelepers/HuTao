using Discord;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Utilities;

namespace HuTao.Services.Moderation;

public static class ModerationActionExtensions
{
    public static EmbedBuilder WithTimestamp(this EmbedBuilder builder, IModerationAction action, bool useFooter = true)
        => builder.WithTimestamp(action.Action, useFooter);

    public static string GetDate(this ModerationAction action)
        => action.Date.ToUniversalTimestamp();

    public static string GetDate(this IModerationAction action)
        => action.Action?.GetDate() ?? "Unknown";

    public static string GetLatestReason(this Reprimand action, int length = 256)
        => action.ModifiedAction?.GetReason(length) ?? action.Action?.GetReason(length) ?? "No reason.";

    public static string GetModerator(this ModerationAction action)
        => $"{Format.Bold(action.MentionUser())} ({action.UserId})";

    public static string GetModerator(this IModerationAction action)
        => action.Action?.GetModerator() ?? "Unknown";

    public static string GetReason(this ModerationAction action, int length = 256)
        => (action.Reason ?? "No reason").Truncate(length);

    public static string GetReason(this IModerationAction action, int length = 256)
        => action.Action?.GetReason(length) ?? "No reason.";

    private static EmbedBuilder WithTimestamp(this EmbedBuilder builder, ModerationAction? action,
        bool useFooter = true)
    {
        if (action is not null) builder.WithTimestamp(action.Date);
        return useFooter ? builder.WithFooter(action?.Date.Humanize()) : builder;
    }
}