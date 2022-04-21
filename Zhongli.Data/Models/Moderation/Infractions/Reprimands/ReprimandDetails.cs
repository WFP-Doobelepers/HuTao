using System.Threading.Tasks;
using Discord;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands;

public record ReprimandDetails(
        IUser User, IGuildUser Moderator,
        string? Reason, Trigger? Trigger = null,
        Context? Context = null, ReprimandResult? Result = null)
    : ActionDetails(Moderator.Id, Moderator.Guild.Id, Reason)
{
    public ReprimandDetails(
        IUser user, Context context,
        string? reason, Trigger? trigger = null,
        ReprimandResult? result = null)
        : this(user, (IGuildUser) context.User,
            reason, trigger,
            context, result) { }

    public IGuild Guild => Moderator.Guild;

    public async Task<IGuildUser?> GetUserAsync() => await Moderator.Guild.GetUserAsync(User.Id);
}