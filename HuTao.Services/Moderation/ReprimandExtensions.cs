using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Exclusions;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.Moderation;

public static class ReprimandExtensions
{
    public static (long Active, long Total) HistoryCount<T>(this GuildUserEntity user, ModerationCategory? category)
        where T : Reprimand
    {
        var reprimands = user.Reprimands<T>(category).ToList();
        return (reprimands.Count(IsCounted), reprimands.Count);
    }

    public static (long Active, long Total) WarningCount(this GuildUserEntity user, ModerationCategory? category)
    {
        var reprimands = user.Reprimands<Warning>(category).ToList();
        return (reprimands.Where(IsCounted).Sum(w => w.Count), reprimands.Sum(w => w.Count));
    }

    public static bool IsActive(this IExpirable expirable)
        => expirable.EndedAt is null || expirable.ExpireAt > DateTimeOffset.UtcNow;

    public static bool IsIncluded(this Reprimand reprimand, LogReprimandType log)
    {
        if (log == LogReprimandType.All)
            return true;

        return reprimand switch
        {
            Ban           => log.HasFlag(LogReprimandType.Ban),
            Censored      => log.HasFlag(LogReprimandType.Censored),
            Filtered      => log.HasFlag(LogReprimandType.Filtered),
            HardMute      => log.HasFlag(LogReprimandType.HardMute),
            Kick          => log.HasFlag(LogReprimandType.Kick),
            Mute          => log.HasFlag(LogReprimandType.Mute),
            Note          => log.HasFlag(LogReprimandType.Note),
            Notice        => log.HasFlag(LogReprimandType.Notice),
            RoleReprimand => log.HasFlag(LogReprimandType.Role),
            Warning       => log.HasFlag(LogReprimandType.Warning),
            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "This reprimand type cannot be logged.")
        };
    }

    public static bool IsIncluded<T>(this Reprimand reprimand, LogConfig<T> config) where T : ModerationLogConfig
        => reprimand.IsIncluded(config.LogReprimands) && reprimand.IsIncluded(config.LogReprimandStatus);

    public static Color GetColor(this ReprimandAction reprimand) => reprimand switch
    {
        BanAction      => Color.Red,
        HardMuteAction => Color.DarkOrange,
        KickAction     => Color.Red,
        MuteAction     => Color.Orange,
        NoteAction     => Color.Blue,
        NoticeAction   => Color.Gold,
        RoleAction     => Color.Orange,
        WarningAction  => Color.Gold,
        _ => throw new ArgumentOutOfRangeException(
            nameof(reprimand), reprimand, "An unknown reprimand was given.")
    };

    public static Color GetColor(this Reprimand reprimand)
    {
        if (reprimand.Status is ReprimandStatus.Deleted)
            return Color.Red;

        if (reprimand.Status is not ReprimandStatus.Added)
            return Color.Purple;

        return reprimand switch
        {
            Ban           => Color.Red,
            Censored      => Color.Blue,
            Filtered      => Color.Blue,
            HardMute      => Color.DarkOrange,
            Kick          => Color.Red,
            Mute          => Color.Orange,
            Note          => Color.Blue,
            Notice        => Color.Gold,
            RoleReprimand => Color.Orange,
            Warning       => Color.Gold,

            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "An unknown reprimand was given.")
        };
    }

    public static ComponentBuilder ToComponentBuilder(this Reprimand reprimand, bool ephemeral = false)
    {
        var components = new ComponentBuilder();

        if (reprimand.Status is ReprimandStatus.Deleted) return components;

        components.WithButton("Update", $"reprimand-update:{reprimand.Id}:{ephemeral}", ButtonStyle.Secondary);

        if (reprimand is ExpirableReprimand && reprimand.IsCounted())
            components.WithButton("Pardon", $"reprimand-pardon:{reprimand.Id}:{ephemeral}", ButtonStyle.Secondary);

        return components.WithButton("Delete", $"reprimand-delete:{reprimand.Id}:{ephemeral}", ButtonStyle.Danger);
    }

    public static EmbedBuilder ToEmbedBuilder(this ModerationCategory category) => new EmbedBuilder()
        .WithTitle($"Category: {category.Id}")
        .AddField("Name", category.Name, true)
        .AddField("Authorization", category.Authorization.Humanize().DefaultIfNullOrEmpty("None"), true);

    public static EmbedBuilder ToEmbedBuilder(this Reprimand r, bool showId, int? length = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{r.Status} {r.GetTitle(showId)}")
            .WithDescription(r.GetReason(length ?? EmbedFieldBuilder.MaxFieldValueLength))
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

        if (r.Category is not null)
            embed.AddField("Category", r.Category.Name, true);

        if (r.Trigger is not null)
            embed.AddField($"Triggers on {r.Trigger.GetTitle()}", r.Trigger.GetDetails());

        return embed;
    }

    public static EmbedBuilder WithExpirableDetails(this EmbedBuilder builder, ExpirableReprimand reprimand) => builder
        .WithTitle(reprimand.GetTitle(true))
        .AddField("User", $"{reprimand.MentionUser()} ({reprimand.UserId})", true)
        .AddField("Category", reprimand.Category?.Name ?? "None", true)
        .AddField("Expiry", reprimand.GetExpirationTime(), true)
        .WithColor(reprimand.Length is null ? Color.DarkOrange : Color.Orange);

    public static IEnumerable<Reprimand> OfCategory(
        this IEnumerable<Reprimand> reprimands, ModerationCategory category) => category switch
    {
        _ when category == ModerationCategory.All  => reprimands,
        _ when category == ModerationCategory.None => reprimands.Where(r => r.Category is null),
        _                                          => reprimands.Where(r => r.Category?.Id == category.Id)
    };

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
                LogReprimandType.Role     => reprimands.OfType<RoleReprimand>(),
                LogReprimandType.HardMute => reprimands.OfType<HardMute>(),
                LogReprimandType.Filtered => reprimands.OfType<Filtered>(),
                _                         => Enumerable.Empty<Reprimand>()
            });
    }

    public static IEnumerable<T> Reprimands<T>(this GuildUserEntity user, ModerationCategory? category)
        where T : Reprimand
        => user.Guild.ReprimandHistory
            .Where(r => r.UserId == user.Id && r.Status is not ReprimandStatus.Deleted)
            .OfCategory(category ?? ModerationCategory.None).OfType<T>();

    public static string GetAction(this Reprimand action)
    {
        var mention = action.MentionUser();
        var status = action.Status;

        return action switch
        {
            Ban b           => $"{status} ban to {mention} for {b.GetLength()}.",
            Censored c      => $"{status} censor to {mention}: {c.CensoredMessage().Truncate(512)}",
            Filtered f      => $"{status} filter to {mention}: {f.Messages.Humanize().Truncate(512)}",
            HardMute h      => $"{status} hard mute to {mention} for {h.GetLength()}.",
            Kick            => $"{status} kick to {mention}.",
            Mute m          => $"{status} mute to {mention} for {m.GetLength()}.",
            Note            => $"{status} note to {mention}.",
            Notice          => $"{status} notice to {mention}.",
            RoleReprimand r => $"{status} roles to {mention} for {r.GetLength()}: {((IRoleReprimand) r).Humanized}",
            Warning w       => $"{status} warn to {mention} {w.Count} times.",

            _ => throw new ArgumentOutOfRangeException(
                nameof(action), action, "An unknown reprimand was given.")
        };
    }

    public static string GetDetails(this ModerationExclusion exclusion, Context context) => exclusion switch
    {
        InviteExclusion e      => $"Guild: {context.GetGuild(e.GuildId)} ({e.Guild.Id})",
        EmojiExclusion e       => $"Emoji: {e.Emoji}",
        CriterionExclusion e   => $"Criterion: {e.Criterion}",
        LinkExclusion e        => $"Link: {e.Link.Uri}",
        RoleMentionExclusion e => $"Role: {e.MentionRole()}",
        UserMentionExclusion e => $"User: {e.MentionUser()}",
        _ => throw new ArgumentOutOfRangeException(
            nameof(exclusion), exclusion, "An unknown exclusion was given.")
    };

    public static string GetTitle(this Reprimand action, bool showId)
    {
        var title = action switch
        {
            Ban           => nameof(Ban),
            Censored      => nameof(Censored),
            Filtered      => nameof(Filtered),
            HardMute      => nameof(HardMute),
            Kick          => nameof(Kick),
            Mute          => nameof(Mute),
            Note          => nameof(Note),
            Notice        => nameof(Notice),
            RoleReprimand => nameof(RoleReprimand),
            Warning       => nameof(Warning),

            _ => throw new ArgumentOutOfRangeException(
                nameof(action), action, "An unknown reprimand was given.")
        };

        return showId ? $"{title.Humanize()}: {action.Id}" : title.Humanize();
    }

    public static async Task<(long Active, long Total)> CountAsync<T>(
        this T reprimand, Trigger trigger, DbContext db,
        CancellationToken cancellationToken = default) where T : Reprimand
    {
        var user = await reprimand.GetUserAsync(db, cancellationToken);
        var reprimands = user.Reprimands<T>(trigger.Category).Where(r => r.TriggerId == trigger.Id).ToList();
        return (reprimands.Count(IsCounted), reprimands.Count);
    }

    public static async Task<RoleTemplateResult> AddRolesAsync(
        this IGuildUser user, ICollection<RoleTemplate> templates,
        CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions { CancelToken = cancellationToken };
        var added = new List<RoleMetadata>();
        var removed = new List<RoleMetadata>();

        foreach (var add in templates.Where(t => t.Behavior is RoleBehavior.Add))
        {
            try
            {
                await user.AddRoleAsync(add.RoleId, options);
                added.Add(new RoleMetadata(add, user));
            }
            catch (HttpException)
            {
                // Ignored
            }
        }

        foreach (var remove in templates.Where(t => t.Behavior is RoleBehavior.Remove))
        {
            try
            {
                await user.RemoveRoleAsync(remove.RoleId, options);
                removed.Add(new RoleMetadata(remove, user));
            }
            catch (HttpException)
            {
                // Ignored
            }
        }

        foreach (var toggle in templates.Where(t => t.Behavior is RoleBehavior.Toggle))
        {
            try
            {
                if (user.HasRole(toggle.RoleId))
                {
                    await user.RemoveRoleAsync(toggle.RoleId, options);
                    removed.Add(new RoleMetadata(toggle, user));
                }
                else
                {
                    await user.AddRoleAsync(toggle.RoleId, options);
                    added.Add(new RoleMetadata(toggle, user));
                }
            }
            catch (HttpException)
            {
                // Ignored
            }
        }

        return new RoleTemplateResult(added, removed);
    }

    public static async Task<T?> GetActive<T>(this DbContext db, ReprimandDetails details,
        CancellationToken cancellationToken = default) where T : ExpirableReprimand
    {
        var entities = await db.Set<T>()
            .Where(r => r.UserId == details.User.Id && r.GuildId == details.Guild.Id)
            .Where(r => details.Category == null
                || (r.Category != null && r.Category.Id == details.Category.Id))
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

    public static async ValueTask<(long Active, long Total)> CountUserReprimandsAsync(
        this Reprimand reprimand, DbContext db,
        CancellationToken cancellationToken = default)
    {
        var user = await reprimand.GetUserAsync(db, cancellationToken);

        return reprimand switch
        {
            Ban           => Count<Ban>(),
            Censored      => Count<Censored>(),
            Filtered      => Count<Filtered>(),
            HardMute      => Count<HardMute>(),
            Kick          => Count<Kick>(),
            Mute          => Count<Mute>(),
            Note          => Count<Note>(),
            Notice        => Count<Notice>(),
            RoleReprimand => Count<RoleReprimand>(),
            Warning       => user.WarningCount(reprimand.Category),

            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "An unknown reprimand was given.")
        };

        (long Active, long Total) Count<T>() where T : Reprimand => user.HistoryCount<T>(reprimand.Category);
    }

    public static ValueTask<GuildEntity> GetGuildAsync(this ReprimandDetails details, HuTaoContext db,
        CancellationToken cancellationToken)
        => db.Guilds.TrackGuildAsync(details.Guild, cancellationToken);

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

    private static bool IsCounted(this Reprimand reprimand)
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
            ReprimandStatus.Pardoned => status.HasFlag(LogReprimandStatus.Pardoned),
            ReprimandStatus.Deleted  => status.HasFlag(LogReprimandStatus.Deleted),
            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "This reprimand type cannot be logged.")
        };
    }

    private static string GetExpirationTime(this IExpirable expirable)
        => expirable.ExpireAt?.ToUniversalTimestamp() ?? "Indefinitely";

    private static string GetGuild(this Context context, ulong guildId) => context.Client is DiscordSocketClient client
        ? client.GetGuild(guildId)?.Name ?? $"[Unknown Guild] ({guildId})"
        : $"[Unknown Name] {guildId}";

    private static string GetLength(this ILength mute)
        => mute.Length?.Humanize(5,
            minUnit: TimeUnit.Second,
            maxUnit: TimeUnit.Year) ?? "indefinitely";

    public record RoleTemplateResult(
        IReadOnlyCollection<RoleMetadata> Added,
        IReadOnlyCollection<RoleMetadata> Removed)
    {
        public IEnumerable<RoleMetadata> All => Added.Concat(Removed);
    }
}