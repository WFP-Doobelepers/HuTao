using Discord;
using Discord.Commands;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public readonly struct ReprimandDetails
    {
        public ReprimandDetails(IGuildUser user, ModerationActionType type, string? reason = null)
        {
            GuildId     = user.GuildId;
            UserId      = user.Id;
            
            Type        = type;
            Reason      = reason;
        }

        public ulong GuildId { get; }
        

        public ulong UserId { get; }

        public ModerationActionType Type { get; }

        public string? Reason { get; }
    }
}