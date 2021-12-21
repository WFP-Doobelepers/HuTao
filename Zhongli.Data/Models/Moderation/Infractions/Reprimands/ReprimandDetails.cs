using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands;

public record ReprimandDetails(IUser User, IGuildUser Moderator, string? Reason, Trigger? Trigger = null, ICommandContext? Context = null)
    : ActionDetails(Moderator.Id, Moderator.Guild.Id, Reason)
{
    public ReprimandDetails(IUser user, ICommandContext context, string? reason, Trigger? trigger = null)
        : this(user, (IGuildUser) context.User, reason, trigger, context) { }

    public IGuild Guild => Moderator.Guild;

    public async Task<IGuildUser?> GetUserAsync() => await Moderator.Guild.GetUserAsync(User.Id);
}