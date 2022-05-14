using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions.Triggers;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public record ReprimandDetails(
        IUser User, IGuildUser Moderator,
        string? Reason, Trigger? Trigger = null,
        Context? Context = null, ModerationCategory? Category = null,
        ReprimandResult? Result = null)
    : ActionDetails(Moderator.Id, Moderator.Guild.Id, Reason)
{
    public ReprimandDetails(
        Context context, IUser user,
        string? reason, Trigger? trigger = null,
        ModerationCategory? category = null,
        ReprimandResult? result = null)
        : this(user, (IGuildUser) context.User, reason, trigger, context,
            category == ModerationCategory.Default ? null : category ?? trigger?.Category, result) { }

    public IGuild Guild => Moderator.Guild;

    public async Task<IGuildUser?> GetUserAsync() => await Moderator.Guild.GetUserAsync(User.Id);
}