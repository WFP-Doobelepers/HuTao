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
