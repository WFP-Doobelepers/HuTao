using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Services.Core;

namespace HuTao.Services.Moderation;

public static class MessageDeleteAccess
{
    /// <summary>
    ///     Whether the member may delete a message in the channel with a Delete button. The button names the channel and
    ///     the message in its custom ID, and the bot deletes the message with its own permissions, so the member needs
    ///     what the purge commands need: the Purge scope, and Discord's Manage Messages permission in that channel.
    /// </summary>
    public static async Task<bool> MayDeleteAsync(
        this AuthorizationService auth, Context context, IGuildUser user, IGuildChannel channel)
        => user.GetPermissions(channel).ManageMessages
            && await auth.IsAuthorizedAsync(context, AuthorizationScope.Purge | AuthorizationScope.All);
}
