using Discord;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public record ReprimandDetails(IGuildUser User, IGuildUser Moderator, string? Reason, Trigger? Trigger = null)
        : ActionDetails(Moderator.Id, Moderator.Guild.Id, Reason);

    public record ModifiedReprimand(IUser User, IGuildUser Moderator, string? Reason, Trigger? Trigger = null)
        : ActionDetails(Moderator.Id, Moderator.Guild.Id, Reason);
}