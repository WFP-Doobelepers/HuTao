using Discord;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public readonly struct ReprimandDetails
    {
        public ReprimandDetails(IGuildUser user, IGuildUser moderator, ModerationActionType type, string? reason)
        {
            User      = user;
            Moderator = moderator;

            Type   = type;
            Reason = reason;
        }

        public IGuildUser Moderator { get; }

        public IGuildUser User { get; }

        public ModerationActionType Type { get; }

        public string? Reason { get; }
    }
}