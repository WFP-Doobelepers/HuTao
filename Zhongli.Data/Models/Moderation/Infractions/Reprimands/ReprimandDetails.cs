using Discord;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public record ReprimandDetails(IGuildUser User, IGuildUser Moderator,
        ModerationSource Type, string? Reason);

    public record ModifiedReprimand(IUser User, IGuildUser Moderator,
        ModerationSource Type, string? Reason);
}