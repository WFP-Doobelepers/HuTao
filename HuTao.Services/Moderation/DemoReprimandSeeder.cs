using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using TimeoutReprimand = HuTao.Data.Models.Moderation.Infractions.Reprimands.Timeout;

namespace HuTao.Services.Moderation;

public class DemoReprimandSeeder(HuTaoContext db)
{
    public const ulong TestGuildId = 923991820868911184ul;

    public async Task<DemoSeedResult> SeedAsync(
        IGuild guild,
        IGuildUser moderator,
        IReadOnlyList<IGuildUser> targetUsers,
        DemoSeedOptions options,
        CancellationToken cancellationToken = default)
    {
        if (guild.Id != TestGuildId)
            throw new InvalidOperationException($"Demo seeding is only enabled for guild {TestGuildId}.");

        if (options.MinReprimandsPerUser < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MinReprimandsPerUser must be >= 0.");
        if (options.MaxReprimandsPerUser < options.MinReprimandsPerUser)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxReprimandsPerUser must be >= MinReprimandsPerUser.");
        if (options.DaysBack <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "DaysBack must be > 0.");

        var guildEntity = await db.Guilds.TrackGuildAsync(guild, cancellationToken);
        _ = guildEntity;

        var moderatorEntity = await db.Users.TrackUserAsync(moderator, cancellationToken);
        moderatorEntity.JoinedAt ??= moderator.JoinedAt?.ToUniversalTime();

        foreach (var u in targetUsers)
        {
            var entity = await db.Users.TrackUserAsync(u, cancellationToken);
            entity.JoinedAt ??= u.JoinedAt?.ToUniversalTime();
        }

        if (options.ClearExisting)
        {
            var ids = targetUsers.Select(u => u.Id).ToArray();
            var existing = await db.Set<Reprimand>()
                .Where(r => r.GuildId == guild.Id && ids.Contains(r.UserId))
                .ToListAsync(cancellationToken);

            foreach (var r in existing)
            {
                if (r.ModifiedAction is not null)
                    db.Remove(r.ModifiedAction);
                db.Remove(r);
            }
        }

        var userIds = targetUsers.Select(u => u.Id).ToArray();
        var plan = GeneratePlan(userIds, options, DateTimeOffset.UtcNow);

        var created = 0;
        foreach (var item in plan)
        {
            var user = targetUsers.FirstOrDefault(u => u.Id == item.UserId);
            if (user is null)
                continue;

            var details = new ReprimandDetails(user, moderator, item.Reason);
            var reprimand = CreateReprimand(details, item);

            if (reprimand.Action is not null)
                reprimand.Action.Date = item.ActionDate;

            if (reprimand is ExpirableReprimand expirable)
            {
                expirable.StartedAt = item.ActionDate;
                expirable.Length = item.Length;
                expirable.ExpireAt = item.Length is null ? null : item.ActionDate + item.Length;
                expirable.EndedAt = item.EndedAt;
            }

            reprimand.Status = item.Status;
            if (item.Modified is not null)
            {
                reprimand.ModifiedAction = new ReprimandDetails(user, moderator, item.Modified.Reason);
                if (reprimand.ModifiedAction is not null)
                    reprimand.ModifiedAction.Date = item.Modified.Date;
            }

            db.Add(reprimand);
            created++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new DemoSeedResult(targetUsers.Count, created, options.ClearExisting);
    }

    internal static IReadOnlyList<DemoSeedItem> GeneratePlan(
        IReadOnlyList<ulong> userIds,
        DemoSeedOptions options,
        DateTimeOffset now)
    {
        var rng = new Random(options.Seed);
        var results = new List<DemoSeedItem>(userIds.Count * Math.Max(1, options.MaxReprimandsPerUser));

        foreach (var userId in userIds)
        {
            var count = rng.Next(options.MinReprimandsPerUser, options.MaxReprimandsPerUser + 1);
            for (var i = 0; i < count; i++)
            {
                var kind = PickKind(rng);
                var date = now - TimeSpan.FromDays(rng.Next(0, options.DaysBack)) - TimeSpan.FromHours(rng.Next(0, 24));

                var (reason, length, extra) = BuildDetails(kind, rng);
                var status = PickStatus(kind, length, date, now, rng);
                var endAt = status is ReprimandStatus.Expired && length is not null ? date + length : null;

                ModifiedSeed? modified = null;
                if (status is ReprimandStatus.Updated or ReprimandStatus.Pardoned or ReprimandStatus.Deleted or ReprimandStatus.Expired)
                {
                    var delta = TimeSpan.FromHours(rng.Next(1, 72));
                    modified = new ModifiedSeed(date + delta, status switch
                    {
                        ReprimandStatus.Updated => "Updated after review",
                        ReprimandStatus.Pardoned => "Pardoned after appeal",
                        ReprimandStatus.Deleted => "Deleted (demo data cleanup)",
                        ReprimandStatus.Expired => "[Reprimand Expired]",
                        _ => "Modified"
                    });
                }

                results.Add(new DemoSeedItem(
                    userId,
                    kind,
                    status,
                    date,
                    length,
                    endAt,
                    reason,
                    extra,
                    modified));
            }
        }

        return results
            .OrderByDescending(r => r.ActionDate)
            .ToList();
    }

    private static Reprimand CreateReprimand(ReprimandDetails details, DemoSeedItem item)
    {
        return item.Kind switch
        {
            DemoReprimandKind.Warning => new Warning((uint) item.Extra.WarningCount, item.Length, details),
            DemoReprimandKind.Note => new Note(details),
            DemoReprimandKind.Notice => new Notice(item.Length, details),
            DemoReprimandKind.Timeout => new TimeoutReprimand(item.Length, details),
            DemoReprimandKind.Mute => new Mute(item.Length, details),
            DemoReprimandKind.Kick => new Kick(details),
            DemoReprimandKind.Ban => new Ban((uint) item.Extra.BanDeleteDays, item.Length, details),
            DemoReprimandKind.Censored => new Censored(item.Extra.CensoredContent, item.Length, details),
            _ => new Note(details)
        };
    }

    private static DemoReprimandKind PickKind(Random rng)
        => KindWeights[rng.Next(0, KindWeights.Length)];

    private static ReprimandStatus PickStatus(
        DemoReprimandKind kind,
        TimeSpan? length,
        DateTimeOffset actionDate,
        DateTimeOffset now,
        Random rng)
    {
        var roll = rng.NextDouble();

        if (roll < 0.75)
            return ReprimandStatus.Added;
        if (roll < 0.85)
            return ReprimandStatus.Updated;
        if (roll < 0.92)
            return ReprimandStatus.Pardoned;
        if (roll < 0.97)
            return ReprimandStatus.Deleted;

        if (kind is DemoReprimandKind.Warning or DemoReprimandKind.Notice or DemoReprimandKind.Timeout or DemoReprimandKind.Mute or DemoReprimandKind.Ban or DemoReprimandKind.Censored)
        {
            if (length is not null && actionDate + length < now - TimeSpan.FromHours(1))
                return ReprimandStatus.Expired;
        }

        return ReprimandStatus.Added;
    }

    private static (string Reason, TimeSpan? Length, DemoSeedExtra Extra) BuildDetails(DemoReprimandKind kind, Random rng)
    {
        return kind switch
        {
            DemoReprimandKind.Warning => (Pick(WarningReasons, rng), TimeSpan.FromDays(rng.Next(7, 31)), new DemoSeedExtra(WarningCount: rng.Next(1, 4))),
            DemoReprimandKind.Note => (Pick(NoteReasons, rng), null, DemoSeedExtra.Default),
            DemoReprimandKind.Notice => (Pick(NoticeReasons, rng), TimeSpan.FromDays(rng.Next(3, 15)), DemoSeedExtra.Default),
            DemoReprimandKind.Timeout => (Pick(TimeoutReasons, rng), TimeSpan.FromMinutes(rng.Next(10, 60 * 24)), DemoSeedExtra.Default),
            DemoReprimandKind.Mute => (Pick(MuteReasons, rng), TimeSpan.FromHours(rng.Next(1, 72)), DemoSeedExtra.Default),
            DemoReprimandKind.Kick => (Pick(KickReasons, rng), null, DemoSeedExtra.Default),
            DemoReprimandKind.Ban => (Pick(BanReasons, rng), rng.NextDouble() < 0.7 ? null : TimeSpan.FromDays(rng.Next(1, 14)), new DemoSeedExtra(BanDeleteDays: rng.Next(1, 8))),
            DemoReprimandKind.Censored => ("Auto-censor triggered", TimeSpan.FromDays(rng.Next(1, 8)), new DemoSeedExtra(CensoredContent: Pick(CensoredContents, rng))),
            _ => ("No reason provided", null, DemoSeedExtra.Default)
        };
    }

    private static string Pick(string[] options, Random rng) => options[rng.Next(0, options.Length)];

    private static readonly DemoReprimandKind[] KindWeights =
    [
        DemoReprimandKind.Warning,
        DemoReprimandKind.Warning,
        DemoReprimandKind.Warning,
        DemoReprimandKind.Note,
        DemoReprimandKind.Note,
        DemoReprimandKind.Notice,
        DemoReprimandKind.Timeout,
        DemoReprimandKind.Mute,
        DemoReprimandKind.Kick,
        DemoReprimandKind.Ban,
        DemoReprimandKind.Censored
    ];

    private static readonly string[] WarningReasons =
    [
        "Spamming in #general",
        "Off-topic posting after warning",
        "Excessive caps / emoji spam",
        "Ignoring moderator instructions",
        "Posting spoilers without tags"
    ];

    private static readonly string[] NoteReasons =
    [
        "User apologized; keep an eye on behavior",
        "Prior incident referenced in appeal",
        "Friendly reminder issued in DMs",
        "Context: conversation escalated quickly"
    ];

    private static readonly string[] NoticeReasons =
    [
        "Final reminder to follow server rules",
        "Notice issued for repeated minor issues",
        "Keep discussion civil and on-topic"
    ];

    private static readonly string[] TimeoutReasons =
    [
        "Heated argument - cooldown period",
        "Harassment / personal attacks",
        "Disruptive behavior in voice chat",
        "Repeated rule-breaking after warnings"
    ];

    private static readonly string[] MuteReasons =
    [
        "Continued spamming after warnings",
        "Bypassing slowmode",
        "Repeatedly pinging roles / users",
        "Posting low-effort spam"
    ];

    private static readonly string[] KickReasons =
    [
        "Repeated rule violations",
        "Aggressive behavior toward members",
        "Evading moderation actions"
    ];

    private static readonly string[] BanReasons =
    [
        "Hate speech / slurs",
        "Scam / phishing attempts",
        "Raid participation",
        "Severe harassment",
        "Advertising other servers after warnings"
    ];

    private static readonly string[] CensoredContents =
    [
        "some very rude message",
        "spoilers without tags",
        "invite link",
        "NSFW content",
        "scam link"
    ];
}

public record DemoSeedOptions(
    int MinReprimandsPerUser = 3,
    int MaxReprimandsPerUser = 10,
    int DaysBack = 120,
    int Seed = 923991820)
{
    public bool ClearExisting { get; init; }
}

public record DemoSeedResult(int UsersSeeded, int ReprimandsCreated, bool ClearedExisting);

internal enum DemoReprimandKind
{
    Warning,
    Note,
    Notice,
    Timeout,
    Mute,
    Kick,
    Ban,
    Censored
}

internal record DemoSeedItem(
    ulong UserId,
    DemoReprimandKind Kind,
    ReprimandStatus Status,
    DateTimeOffset ActionDate,
    TimeSpan? Length,
    DateTimeOffset? EndedAt,
    string Reason,
    DemoSeedExtra Extra,
    ModifiedSeed? Modified);

internal record ModifiedSeed(DateTimeOffset Date, string Reason);

internal record DemoSeedExtra(
    int WarningCount = 1,
    int BanDeleteDays = 1,
    string CensoredContent = "")
{
    public static DemoSeedExtra Default { get; } = new();
}

