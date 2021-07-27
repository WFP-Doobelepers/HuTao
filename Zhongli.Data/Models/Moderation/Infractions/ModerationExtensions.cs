using Discord;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public static class ModerationExtensions
    {
        public static T WithModerator<T>(this T action, IGuildUser moderator) where T : IModerationAction
        {
            action.Action = new ModerationAction(moderator, null);

            return action;
        }
    }
}