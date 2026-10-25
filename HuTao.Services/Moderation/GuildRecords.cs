using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.Moderation;

/// <summary>
///     Looks up a record by an ID that a button or a form carries. A member of another server can send one with any ID,
///     so the lookup finds only this server's records.
/// </summary>
public static class GuildRecords
{
    /// <summary>The guild's reprimand with this ID, with its action and category, or null.</summary>
    public static Task<T?> ReprimandAsync<T>(
        this DbContext db, ulong guildId, Guid id, CancellationToken cancellationToken = default) where T : Reprimand
        => db.Set<T>()
            .Include(r => r.Action)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && r.GuildId == guildId, cancellationToken);

    /// <summary>The guild's trigger with this ID, or null.</summary>
    public static T? Trigger<T>(this GuildEntity guild, Guid id) where T : Trigger
        => guild.ModerationRules?.Triggers.OfType<T>().FirstOrDefault(t => t.Id == id);
}
