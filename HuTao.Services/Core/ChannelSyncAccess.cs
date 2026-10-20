using Discord;

namespace HuTao.Services.Core;

/// <summary>
///     Syncing a channel to its category replaces the channel's own overwrites, which can drop the deny that keeps a
///     private channel private. A member may only sync a channel they can see; the guild owner and administrators may sync
///     any.
/// </summary>
public static class ChannelSyncAccess
{
    public static string? Check(IGuildUser member, IGuildChannel channel)
        => member.Guild?.OwnerId == member.Id
            || member.GuildPermissions.Administrator
            || member.GetPermissions(channel).ViewChannel
                ? null
                : $"You cannot sync {channel}, because you cannot see it.";
}
