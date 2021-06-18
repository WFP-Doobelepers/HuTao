using Discord;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public readonly struct ReprimandDetails
    {
        public ReprimandDetails(IGuildUser user, IUser moderator, ModerationActionType type, string? reason = null)
        {
            GuildId     = user.GuildId;
            ModeratorId = moderator.Id;
            UserId      = user.Id;
            Type        = type;
            Reason      = reason;
        }

        public ulong GuildId { get; }

        public ulong ModeratorId { get; }

        public ulong UserId { get; }

        public ModerationActionType Type { get; }

        public string? Reason { get; }
    }
}