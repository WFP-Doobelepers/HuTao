using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Humanizer.Localisation;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.Moderation;

public static class ReprimandExtensions
{
    public static bool IsActive(this IExpirable expirable)
        => expirable.EndedAt is null || expirable.ExpireAt > DateTimeOffset.UtcNow;

    public static bool IsIncluded(this Reprimand reprimand, LogReprimandType log)
    {
        if (log == LogReprimandType.All)
            return true;

        return reprimand switch
        {
            Ban      => log.HasFlag(LogReprimandType.Ban),
            Censored => log.HasFlag(LogReprimandType.Censored),
            Kick     => log.HasFlag(LogReprimandType.Kick),
            Mute     => log.HasFlag(LogReprimandType.Mute),
            Note     => log.HasFlag(LogReprimandType.Note),
            Notice   => log.HasFlag(LogReprimandType.Notice),
            Warning  => log.HasFlag(LogReprimandType.Warning),
            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "This reprimand type cannot be logged.")
        };
    }

    public static bool IsIncluded(this Reprimand reprimand, ModerationLogConfig config)
        => reprimand.IsIncluded(config.LogReprimands) && reprimand.IsIncluded(config.LogReprimandStatus);

    public static Color GetColor(this Reprimand reprimand)
    {
        if (reprimand.Status is not ReprimandStatus.Added)
            return Color.Purple;

        return reprimand switch
        {
            Ban      => Color.Red,
            Censored => Color.Blue,
            Kick     => Color.Red,
            Mute     => Color.Orange,
            Note     => Color.Blue,
            Notice   => Color.Gold,
            Warning  => Color.Gold,

            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "An unknown reprimand was given.")
        };
    }

    public static EmbedBuilder ToEmbedBuilder(this Reprimand r, bool showId)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{r.Status} {r.GetTitle(showId)}")
            .WithDescription(r.GetReason().Truncate(EmbedBuilder.MaxDescriptionLength))
            .WithColor(r.GetColor()).WithTimestamp(r)
            .AddField("Reprimand", r.GetAction(), true)
            .AddField("Moderator", r.GetModerator(), true);

        if (r.ModifiedAction is not null)
        {
            embed.AddField(e => e
                .WithName("Modified")
                .WithValue(new StringBuilder()
                    .AppendLine($"▌{r.Status.Humanize()} by {r.ModifiedAction.GetModerator()}")
                    .AppendLine($"▌{r.ModifiedAction.GetDate()}")
                    .AppendLine($"{r.ModifiedAction.GetReason()}")
                    .ToString()));
        }

        if (r.Trigger is not null)
            embed.AddField($"Triggers on {r.Trigger.GetTitle()}", r.Trigger.GetTriggerDetails());

        return embed;
    }

    public static IEnumerable<Reprimand> OfType(this IEnumerable<Reprimand> reprimands, LogReprimandType types)
    {
        if (types is LogReprimandType.All or LogReprimandType.None) return reprimands;

        return Enum.GetValues<LogReprimandType>()
            .Where(t => types.HasFlag(t))
            .SelectMany(t => t switch
            {
                LogReprimandType.Ban      => reprimands.OfType<Ban>(),
                LogReprimandType.Censored => reprimands.OfType<Censored>(),
                LogReprimandType.Kick     => reprimands.OfType<Kick>(),
                LogReprimandType.Mute     => reprimands.OfType<Mute>(),
                LogReprimandType.Note     => reprimands.OfType<Note>(),
                LogReprimandType.Notice   => reprimands.OfType<Notice>(),
                LogReprimandType.Warning  => reprimands.OfType<Warning>(),
                _                         => Enumerable.Empty<Reprimand>()
            });
    }

    public static IEnumerable<T> Reprimands<T>(this GuildUserEntity user, bool countHidden = true)
        where T : Reprimand
    {
        var reprimands = user.Guild.ReprimandHistory.OfType<T>()
            .Where(r => r.UserId == user.Id && r.Status is not ReprimandStatus.Deleted);

        return countHidden
            ? reprimands
            : reprimands.Where(IsCounted);
    }

    public static string GetAction(this Reprimand action)
    {
        var mention = action.MentionUser();
        var status = action.Status;

        return action switch
        {
            Ban b      => $"{status} ban to {mention} for {b.GetLength()}.",
            Censored c => $"{status} censor to {mention}. Message: {c.CensoredMessage().Truncate(512)}",
            Kick       => $"{status} kick to {mention}.",
            Mute m     => $"{status} mute to {mention} for {m.GetLength()}.",
            Note       => $"{status} note to {mention}.",
            Notice     => $"{status} notice to {mention}.",
            Warning w  => $"{status} warn to {mention} {w.Count} times.",

            _ => throw new ArgumentOutOfRangeException(
                nameof(action), action, "An unknown reprimand was given.")
        };
    }

    public static string GetReprimandExpiration(this Reprimand reprimand)
    {
        var expiry = reprimand switch
        {
            Ban b  => $"Expires in: {b.GetExpirationTime()}.",
            Mute m => $"Expires in: {m.GetExpirationTime()}.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "This reprimand is not expirable.")
        };

        return new StringBuilder()
            .AppendLine($"User: {reprimand.MentionUser()}")
            .AppendLine(expiry)
            .ToString();
    }

    public static string GetTitle(this Reprimand action, bool showId)
    {
        var title = action switch
        {
            Ban      => nameof(Ban),
            Censored => nameof(Censored),
            Kick     => nameof(Kick),
            Mute     => nameof(Mute),
            Note     => nameof(Note),
            Notice   => nameof(Notice),
            Warning  => nameof(Warning),

            _ => throw new ArgumentOutOfRangeException(
                nameof(action), action, "An unknown reprimand was given.")
        };

        return showId ? $"{title.Humanize()}: {action.Id}" : title.Humanize();
    }

    public static Task<GuildEntity> GetGuildAsync(this ReprimandDetails details, HuTaoContext db,
        CancellationToken cancellationToken)
        => db.Guilds.TrackGuildAsync(details.Guild, cancellationToken);

    public static async Task<T?> GetActive<T>(this DbContext db, ReprimandDetails details,
        CancellationToken cancellationToken = default) where T : ExpirableReprimand
    {
        var entities = await db.Set<T>()
            .Where(m => m.UserId == details.User.Id && m.GuildId == details.Guild.Id)
            .ToListAsync(cancellationToken);

        return entities.FirstOrDefault(m => m.IsActive());
    }

    public static async Task<T?> GetActive<T>(this DbContext db, IGuildUser user,
        CancellationToken cancellationToken = default) where T : ExpirableReprimand
    {
        var entities = await db.Set<T>()
            .Where(m => m.UserId == user.Id && m.GuildId == user.Id)
            .ToListAsync(cancellationToken);

        return entities.FirstOrDefault(m => m.IsActive());
    }

    public static async Task<uint> CountAsync<T>(
        this T reprimand, Trigger trigger,
        DbContext db, bool countHidden = true,
        CancellationToken cancellationToken = default) where T : Reprimand
    {
        var user = await reprimand.GetUserAsync(db, cancellationToken);
        return (uint) user.Reprimands<T>(countHidden)
            .LongCount(r => r.TriggerId == trigger.Id);
    }

    public static uint HistoryCount<T>(this GuildUserEntity user, bool countHidden = true)
        where T : Reprimand
        => (uint) user.Reprimands<T>(countHidden).LongCount();

    public static uint WarningCount(this GuildUserEntity user, bool countHidden = true)
        => (uint) user.Reprimands<Warning>(countHidden).Sum(w => w.Count);

    public static async ValueTask<GuildEntity> GetGuildAsync(this Reprimand reprimand, DbContext db,
        CancellationToken cancellationToken = default)
        => (reprimand.Guild ??
            await db.FindAsync<GuildEntity>(new object[] { reprimand.GuildId }, cancellationToken))!;

    public static async ValueTask<GuildUserEntity> GetUserAsync(this Reprimand reprimand, DbContext db,
        CancellationToken cancellationToken = default)
        => (reprimand.User ??
            await db.FindAsync<GuildUserEntity>(new object[] { reprimand.UserId, reprimand.GuildId },
                cancellationToken))!;

    public static async ValueTask<T?> GetTriggerAsync<T>(this Reprimand reprimand, DbContext db,
        CancellationToken cancellationToken = default) where T : Trigger
    {
        if (reprimand.Trigger is T trigger)
            return trigger;

        if (reprimand.TriggerId is not null)
            return await db.FindAsync<T>(new object[] { reprimand.TriggerId }, cancellationToken);

        return null;
    }

    public static async ValueTask<uint> GetTotalAsync(this Reprimand reprimand, DbContext db, bool countHidden = true,
        CancellationToken cancellationToken = default)
    {
        var user = await reprimand.GetUserAsync(db, cancellationToken);

        return reprimand switch
        {
            Ban      => user.HistoryCount<Ban>(countHidden),
            Censored => user.HistoryCount<Censored>(countHidden),
            Kick     => user.HistoryCount<Kick>(countHidden),
            Mute     => user.HistoryCount<Mute>(countHidden),
            Note     => user.HistoryCount<Note>(countHidden),
            Notice   => user.HistoryCount<Notice>(countHidden),
            Warning  => user.WarningCount(countHidden),

            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "An unknown reprimand was given.")
        };
    }

    private static bool IsCounted(Reprimand reprimand)
        => reprimand.Status is ReprimandStatus.Added or ReprimandStatus.Updated;

    private static bool IsIncluded(this Reprimand reprimand, LogReprimandStatus status)
    {
        if (status == LogReprimandStatus.All)
            return true;

        return reprimand.Status switch
        {
            ReprimandStatus.Unknown  => false,
            ReprimandStatus.Added    => status.HasFlag(LogReprimandStatus.Added),
            ReprimandStatus.Expired  => status.HasFlag(LogReprimandStatus.Expired),
            ReprimandStatus.Updated  => status.HasFlag(LogReprimandStatus.Updated),
            ReprimandStatus.Pardoned => status.HasFlag(LogReprimandStatus.Hidden),
            ReprimandStatus.Deleted  => status.HasFlag(LogReprimandStatus.Deleted),
            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "This reprimand type cannot be logged.")
        };
    }

    private static string GetExpirationTime(this IExpirable expirable)
    {
        if (expirable.ExpireAt is null || expirable.Length is null) return "Indefinitely";

        var length = expirable.ExpireAt.Value - DateTimeOffset.UtcNow;
        return expirable.ExpireAt > DateTimeOffset.UtcNow
            ? length.Humanize(5, minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year)
            : "Expired";
    }

    private static string GetLength(this ILength mute)
        => mute.Length?.Humanize(5,
            minUnit: TimeUnit.Second,
            maxUnit: TimeUnit.Year) ?? "indefinitely";
}