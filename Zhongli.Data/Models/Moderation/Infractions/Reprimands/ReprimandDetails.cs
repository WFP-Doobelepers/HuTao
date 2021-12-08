using System.Threading.Tasks;
using Discord;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands;

public record ReprimandDetails(IUser User, IGuildUser Moderator, string? Reason, Trigger? Trigger = null)
    : ActionDetails(Moderator.Id, Moderator.Guild.Id, Reason)
{
    public IGuild Guild => Moderator.Guild;

    public async Task<IGuildUser?> GetUserAsync() => await Moderator.Guild.GetUserAsync(User.Id);
}