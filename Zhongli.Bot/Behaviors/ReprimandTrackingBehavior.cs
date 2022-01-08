using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors;

public class ReprimandTrackingBehavior :
    INotificationHandler<UserLeftNotification>,
    INotificationHandler<UserBannedNotification>,
    INotificationHandler<UserUnbannedNotification>,
    INotificationHandler<GuildMemberUpdatedNotification>
{
    private readonly ModerationService _moderation;
    private readonly ZhongliContext _db;

    public ReprimandTrackingBehavior(ModerationService moderation, ZhongliContext db)
    {
        _moderation = moderation;
        _db         = db;
    }

    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var old = notification.OldMember.Value;
        if (old is null) return;

        var user = notification.NewMember;
        var log = await GetAuditLogEntry<MemberUpdateAuditLogData>(user.Guild, ActionType.MemberUpdated,
            b => b.Target, user, cancellationToken);

        // User was timed out
        if (old.TimedOutUntil is null && user.TimedOutUntil is not null && log is not null)
        {
            var details = GetDetails(user, user.Guild, log);
            var mute = _db.Add(new Mute(DateTimeOffset.Now - user.TimedOutUntil, details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            await _moderation.PublishReprimandAsync(mute, details, cancellationToken);
        }
        // Time out expired
        else if (old.TimedOutUntil is not null && user.TimedOutUntil is null)
        {
            var active = await _db.GetActive<Mute>(user, cancellationToken);
            if (active is null) return;

            var details = log is null ? null : GetDetails(user, user.Guild, log);
            await _moderation.ExpireReprimandAsync(active, ReprimandStatus.Hidden, cancellationToken, details);
        }
    }

    public async Task Handle(UserBannedNotification notification, CancellationToken cancellationToken)
    {
        var user = notification.User;
        var log = await GetAuditLogEntry<BanAuditLogData>(notification.Guild, ActionType.Ban,
            b => b.Target, user, cancellationToken);
        if (log is null) return;

        var details = GetDetails(user, notification.Guild, log);
        var ban = _db.Add(new Ban(0, TimeSpan.Zero, details)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        await _moderation.PublishReprimandAsync(ban, details, cancellationToken);
    }

    public async Task Handle(UserLeftNotification notification, CancellationToken cancellationToken)
    {
        var log = await GetAuditLogEntry<KickAuditLogData>(notification.Guild, ActionType.Kick,
            b => b.Target, notification.User, cancellationToken);
        if (log is null) return;

        var details = GetDetails(notification.User, notification.Guild, log);
        var ban = new Kick(details);

        await _moderation.PublishReprimandAsync(ban, details, cancellationToken);
    }

    public async Task Handle(UserUnbannedNotification notification, CancellationToken cancellationToken)
    {
        var user = notification.User;
        var guild = notification.Guild;
        var guildUser = guild.GetUser(user.Id);
        var active = await _db.GetActive<Ban>(guildUser, cancellationToken);

        var log = await GetAuditLogEntry<UnbanAuditLogData>(guild, ActionType.Unban,
            b => b.Target, user, cancellationToken);

        if (log is null || log.User.Id == guild.CurrentUser.Id) return;
        if (active is null)
        {
            var details = GetDetails(user, guild, log);
            active = _db.Add(new Ban(0, null, details)).Entity;
        }

        await _moderation.ExpireReprimandAsync(active, ReprimandStatus.Hidden, cancellationToken);
    }

    private static IAsyncEnumerable<RestAuditLogEntry> GetAuditLogEntries(SocketGuild guild, ActionType type)
    {
        var audits = guild.GetAuditLogsAsync(10, actionType: type).Flatten();
        return audits
            .Where(e => DateTimeOffset.UtcNow - e.CreatedAt < TimeSpan.FromMinutes(1))
            .Catch<RestAuditLogEntry, HttpException>(exception
                => exception.DiscordCode is DiscordErrorCode.InsufficientPermissions
                    ? AsyncEnumerable.Empty<RestAuditLogEntry>()
                    : throw exception);
    }

    private static ReprimandDetails GetDetails(IUser target, SocketGuild guild, IAuditLogEntry entry)
        => new(target, guild.GetUser(entry.User.Id), entry.Reason);

    private static ValueTask<RestAuditLogEntry?> GetAuditLogEntry<T>(SocketGuild guild, ActionType type,
        Func<T, IUser> selector, IUser comparison, CancellationToken cancellationToken)
        => GetAuditLogEntries(guild, type)
            .Where(e => DateTimeOffset.Now - e.CreatedAt < TimeSpan.FromMinutes(10))
            .FirstOrDefaultAsync(e => e.Data is T entry
                && selector(entry).Id == comparison.Id, cancellationToken);
}