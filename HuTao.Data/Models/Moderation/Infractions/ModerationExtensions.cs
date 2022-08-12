using Discord;
using HuTao.Data.Models.Discord;

namespace HuTao.Data.Models.Moderation.Infractions;

public static class ModerationExtensions
{
    public static T WithModerator<T>(this T action, IGuildUser? moderator) where T : IModerationAction
    {
        if (moderator is not null)
            action.Action = new ModerationAction(moderator);

        return action;
    }

    public static T WithModerator<T>(this T action, Context context) where T : IModerationAction
        => action.WithModerator((IGuildUser) context.User);
}