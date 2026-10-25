using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.Moderation;

public static class ReprimandCategories
{
    /// <summary>
    ///     The category a moderation command's reprimand goes in. With no category given, the category check passes on
    ///     the moderator's default category, so the reprimand goes in that category too, not in the server's.
    /// </summary>
    public static async Task<ModerationCategory?> ForModeratorAsync(
        this DbSet<GuildUserEntity> users, IGuildUser moderator, ModerationCategory? given,
        CancellationToken cancellationToken = default)
        => given ?? (await users.TrackUserAsync(moderator, cancellationToken)).DefaultCategory;
}
