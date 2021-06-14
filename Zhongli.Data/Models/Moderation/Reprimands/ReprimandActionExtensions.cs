using System;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public static class ReprimandActionExtensions
    {
        public static Warning ToWarning(this ReprimandAction action, uint amount)
        {
            return action.ToModerationActionInternal<Warning>(w => w.Amount = amount);
        }

        public static Ban ToBan(this ReprimandAction action, uint deleteDays)
        {
            return action.ToModerationActionInternal<Ban>(b => b.DeleteDays = deleteDays);
        }

        public static Kick ToKick(this ReprimandAction action) => action.ToModerationActionInternal<Kick>();

        private static T ToModerationActionInternal<T>(this IModerationAction action, Action<T>? selector = null)
            where T : class, IModerationAction, new()
        {
            var entity = new T
            {
                Date      = action.Date,
                Guild     = action.Guild,
                Moderator = action.Moderator,
                User      = action.User,
                Reason    = action.Reason
            };

            selector?.Invoke(entity);
            return entity;
        }
    }
}