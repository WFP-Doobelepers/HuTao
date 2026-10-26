using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using HuTao.Data;
using HuTao.Data.Config;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation.Infractions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HuTao.Services.Logging;

/// <summary>
///     Deletes the logs of deleted messages and of reactions after the days that Retention:MessageLogDays sets. Its
///     default of 30 days is the longest PRIVACY.md says they are kept.
/// </summary>
public class MessageLogCleanupService(HuTaoContext db, RetentionConfig? retention = null)
{
    public const string JobId = "message-log-cleanup";

    /// <summary>The retention when the config sets none.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(new RetentionConfig().MessageLogDays);

    private readonly TimeSpan _retention = TimeSpan.FromDays((retention ?? new RetentionConfig()).MessageLogDays);

    // Each batch is a handful of statements whatever its size, so a larger one means fewer round trips.
    public const int BatchSize = 1000;

    // A server that kept every log before retention existed has a large backlog. Each hourly run deletes at most this
    // many, so the backlog drains over several runs instead of holding the database for hours.
    public const int MaxPerRun = 100_000;

    // A run on a large backlog can outlast the hour. The next run waits a minute for it to finish, and otherwise fails
    // without a retry and leaves the work to the hour after.
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public Task RunAsync() => DeleteLogsOlderThanAsync(DateTimeOffset.UtcNow - _retention);

    /// <returns>How many reaction logs and deletion logs were deleted.</returns>
    public async Task<int> DeleteLogsOlderThanAsync(
        DateTimeOffset cutoff, CancellationToken cancellationToken = default,
        int maxPerRun = MaxPerRun, int batchSize = BatchSize)
    {
        var total = 0;

        while (total < maxPerRun)
        {
            var take = Math.Min(batchSize, maxPerRun - total);
            var deleted = db.Database.IsRelational()
                ? await DeleteLogBatchAsync(cutoff, take, cancellationToken)
                : await RemoveLogBatchAsync(cutoff, take, cancellationToken);

            if (deleted == 0) break;
            total += deleted;
        }

        if (total > 0)
            Log.Information("[Logging] Deleted {Count} reaction and deletion log(s) older than {Cutoff}", total, cutoff);

        return total;
    }

    // A deletion that a filter reprimand lists belongs to the reprimand, and stays as long as the reprimand does.
    private IQueryable<Guid> ListedByReprimands
        => db.Set<MessageDeleteLog>().Where(m => EF.Property<Guid?>(m, "FilteredId") != null).Select(m => m.Id);

    private async Task<int> DeleteLogBatchAsync(DateTimeOffset cutoff, int take, CancellationToken cancellationToken)
    {
        var reactions = await db.Set<ReactionLog>()
            .Where(l => l.LogDate < cutoff)
            .OrderBy(l => l.LogDate).ThenBy(l => l.Id)
            .Take(take)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var listed = ListedByReprimands;
        var deletions = await db.Set<DeleteLog>()
            .Where(l => l.LogDate < cutoff && !listed.Contains(l.Id))
            .OrderBy(l => l.LogDate).ThenBy(l => l.Id)
            .Take(take)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        if (reactions.Count == 0 && deletions.Count == 0) return 0;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // A bulk deletion goes together with the messages it lists, which can be dated a moment after it. A message that
        // a reprimand also lists stays, and stops pointing at the bulk deletion.
        var parents = deletions.Cast<Guid?>().ToList();
        var children = db.Set<MessageDeleteLog>()
            .Where(m => parents.Contains(EF.Property<Guid?>(m, "MessagesDeleteLogId")));
        await children
            .Where(m => listed.Contains(m.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(m => EF.Property<Guid?>(m, "MessagesDeleteLogId"), (Guid?) null),
                cancellationToken);
        var ids = deletions.Concat(await children.Select(m => m.Id).ToListAsync(cancellationToken)).Distinct().ToList();

        // Each deletion has its own record of who deleted it and why, which nothing else points at.
        var actions = await db.Set<DeleteLog>()
            .Where(l => ids.Contains(l.Id))
            .Select(l => EF.Property<Guid?>(l, "ActionId"))
            .Where(a => a != null)
            .Select(a => a!.Value)
            .ToListAsync(cancellationToken);

        // The messages of a bulk deletion point at it, so they go first.
        await db.Set<MessageDeleteLog>()
            .Where(m => ids.Contains(m.Id) && EF.Property<Guid?>(m, "MessagesDeleteLogId") != null)
            .ExecuteDeleteAsync(cancellationToken);
        await db.Set<DeleteLog>().Where(l => ids.Contains(l.Id)).ExecuteDeleteAsync(cancellationToken);
        await db.Set<ModerationAction>().Where(a => actions.Contains(a.Id)).ExecuteDeleteAsync(cancellationToken);
        await db.Set<ReactionLog>().Where(l => reactions.Contains(l.Id)).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return reactions.Count + ids.Count;
    }

    private async Task<int> RemoveLogBatchAsync(DateTimeOffset cutoff, int take, CancellationToken cancellationToken)
    {
        var reactions = await db.Set<ReactionLog>()
            .Where(l => l.LogDate < cutoff)
            .OrderBy(l => l.LogDate)
            .Take(take)
            .ToListAsync(cancellationToken);

        var listed = ListedByReprimands;
        var deletions = await db.Set<DeleteLog>()
            .Include(l => l.Action)
            .Where(l => l.LogDate < cutoff && !listed.Contains(l.Id))
            .OrderBy(l => l.LogDate)
            .Take(take)
            .ToListAsync(cancellationToken);

        var parents = deletions.Select(l => (Guid?) l.Id).ToList();
        var children = await db.Set<MessageDeleteLog>()
            .Include(m => m.Action)
            .Where(m => parents.Contains(EF.Property<Guid?>(m, "MessagesDeleteLogId")))
            .ToListAsync(cancellationToken);

        foreach (var child in children.Where(c => db.Entry(c).Property<Guid?>("FilteredId").CurrentValue is not null))
            db.Entry(child).Property<Guid?>("MessagesDeleteLogId").CurrentValue = null;

        var removed = deletions
            .Concat(children.Where(c => db.Entry(c).Property<Guid?>("FilteredId").CurrentValue is null))
            .Distinct()
            .ToList();

        if (reactions.Count == 0 && removed.Count == 0) return 0;

        db.RemoveRange(removed.Select(l => l.Action).OfType<ModerationAction>());
        db.RemoveRange(removed);
        db.RemoveRange(reactions);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        return reactions.Count + removed.Count;
    }
}
