using System.Linq;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.VoiceChat;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.VoiceChat;

public static class VoiceChatOwnership
{
    /// <summary>
    ///     The owner of the voice chat now, read from the database. An open panel keeps the owner it opened with, which a
    ///     transfer makes stale.
    /// </summary>
    public static async Task<ulong?> GetOwnerIdAsync(HuTaoContext db, ulong textChannelId)
        => await db.Set<VoiceChatLink>().AsNoTracking()
            .Where(vc => vc.TextChannelId == textChannelId)
            .Select(vc => (ulong?) vc.UserId)
            .FirstOrDefaultAsync();

    /// <summary>
    ///     Why the voice owner cannot kick or ban <paramref name="target" /> from their voice chat, or null when they can.
    ///     A moderator, and a member whose highest role is at or above the owner's, cannot be removed. Two members with no
    ///     roles at all are equals that may remove each other, or no member without a role could use the command.
    /// </summary>
    public static string? CannotRemove(IGuildUser owner, IGuildUser target)
    {
        var permissions = target.GuildPermissions;
        if (permissions.Administrator || permissions.ModerateMembers || permissions.MoveMembers)
            return $"{target.Mention} is a moderator, so you cannot kick or ban them from your voice chat.";

        if (target.Hierarchy >= owner.Hierarchy && target.Hierarchy > 0)
            return $"{target.Mention}'s highest role is at or above yours, so you cannot kick or ban them.";

        return null;
    }

    /// <summary>The @everyone overwrite of the channel with only the given bits changed.</summary>
    public static OverwritePermissions EveryoneOverwrite(
        IGuildChannel channel, IRole everyone,
        PermValue? connect = null, PermValue? viewChannel = null)
        => (channel.GetPermissionOverwrite(everyone) ?? new OverwritePermissions())
            .Modify(connect: connect, viewChannel: viewChannel);

    /// <summary>Takes the old owner's overwrite off the channel and gives the new owner theirs.</summary>
    public static async Task TransferOverwritesAsync(IGuildChannel channel, IGuildUser? oldOwner, IGuildUser newOwner)
    {
        if (oldOwner is not null)
            await channel.RemovePermissionOverwriteAsync(oldOwner);

        await channel.AddPermissionOverwriteAsync(newOwner, new OverwritePermissions(
            manageChannel: PermValue.Allow,
            muteMembers: PermValue.Allow));
    }
}
