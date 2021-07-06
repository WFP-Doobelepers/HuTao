using System;
using Discord.Commands;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public static class ModerationExtensions
    {
        public static T WithContext<T>(this T action, ICommandContext context) where T : IModerationAction
        {
            action.Action = new ModerationAction
            {
                Date = DateTimeOffset.UtcNow,

                GuildId     = context.Guild.Id,
                ModeratorId = context.User.Id
            };

            return action;
        }
    }
}