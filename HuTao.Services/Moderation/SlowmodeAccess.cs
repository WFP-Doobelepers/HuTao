using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Services.Core;

namespace HuTao.Services.Moderation;

public static class SlowmodeAccess
{
    /// <summary>
    ///     Why the member may not change the slowmode of the channel, or null when they may. The command's precondition
    ///     reads Manage Channels in the channel where the command runs, and the command can name another channel, so the
    ///     member needs the Slowmode scope or Manage Channels in the channel that the command changes.
    /// </summary>
    public static async Task<string?> RefuseSlowmodeAsync(this AuthorizationService auth, Context context, ITextChannel channel)
    {
        if (context.User is IGuildUser user && user.GetPermissions(channel).ManageChannel)
            return null;

        if (await auth.IsAuthorizedAsync(context, AuthorizationScope.Slowmode | AuthorizationScope.All))
            return null;

        return $"You need the Slowmode scope, or the Manage Channels permission in {channel.Mention}.";
    }
}
