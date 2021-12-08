using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Expirable;
using Zhongli.Services.Utilities;
using IBan = Zhongli.Data.Models.Moderation.Infractions.IBan;

namespace Zhongli.Services.Moderation;

public class ModerationService : ExpirableService<ExpirableReprimand>
{
    private readonly DiscordSocketClient _client;
    private readonly ModerationLoggingService _logging;
    private readonly ZhongliContext _db;

    public ModerationService(ZhongliContext db, DiscordSocketClient client, ModerationLoggingService logging)
        : base(db)
    {
        _client  = client;
        _logging = logging;
        _db      = db;
    }

    public async Task CensorAsync(SocketMessage message, TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var censored = new Censored(message.Content, length, details);

        _db.Add(censored);
        await _db.SaveChangesAsync(cancellationToken);

        if (details.Trigger is Censor censor)
        {
            if (!censor.Silent) await message.DeleteAsync();

            var triggerCount = await censored.CountAsync(censor, _db, false, cancellationToken);
            if (censor.IsTriggered(triggerCount))
            {
                var triggerDetails = new ReprimandDetails(
                    details.User, details.Moderator, $"[Reprimand Triggered] at {triggerCount}", censor);

                await ReprimandAsync(censor.Reprimand, triggerDetails, cancellationToken);
            }
        }

        await PublishReprimandAsync(censored, details, cancellationToken);
    }

    public async Task ConfigureMuteRoleAsync(IGuild guild, IRole? role)
    {
        var guildEntity = await _db.Guilds.TrackGuildAsync(guild);
        var rules = guildEntity.ModerationRules;
        var roleId = rules.MuteRoleId;

        role ??= guild.Roles.FirstOrDefault(r => r.Id == roleId);
        role ??= guild.Roles.FirstOrDefault(r => r.Name == "Muted");
        role ??= await guild.CreateRoleAsync("Muted", isMentionable: false);

        rules.MuteRoleId = role.Id;
        await _db.SaveChangesAsync();

        var permissions = new OverwritePermissions(
            addReactions: PermValue.Deny,
            sendMessages: PermValue.Deny,
            speak: PermValue.Deny,
            stream: PermValue.Deny);

        foreach (var channel in await guild.GetChannelsAsync())
        {
            await channel.AddPermissionOverwriteAsync(role, permissions);
        }
    }

    public async Task DeleteReprimandAsync(Reprimand reprimand, ReprimandDetails? details,
        CancellationToken cancellationToken = default)
    {
        if (details is not null)
            await UpdateReprimandAsync(reprimand, details, ReprimandStatus.Deleted, cancellationToken);

        if (reprimand.Action is not null)
            _db.Remove(reprimand.Action);

        if (reprimand.ModifiedAction is not null)
            _db.Remove(reprimand.ModifiedAction);

        _db.Remove(reprimand);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTriggerAsync(Trigger trigger, IGuildUser moderator, bool silent)
    {
        var reprimands = await _db.Set<Reprimand>().ToAsyncEnumerable()
            .Where(r => r.TriggerId == trigger.Id)
            .ToListAsync();

        foreach (var reprimand in reprimands)
        {
            await DeleteReprimandAsync(reprimand, silent ? null : GetModified(reprimand));

            ReprimandDetails GetModified(IUserEntity r)
            {
                var user = _client.GetUser(r.UserId);
                return new ReprimandDetails(user, moderator, "[Deleted Trigger]");
            }
        }

        _db.Remove(trigger);
        await _db.SaveChangesAsync();
    }

    public Task HideReprimandAsync(Reprimand reprimand, ReprimandDetails details,
        CancellationToken cancellationToken = default)
        => UpdateReprimandAsync(reprimand, details, ReprimandStatus.Hidden, cancellationToken);

    public async Task ToggleTriggerAsync(Trigger trigger, IGuildUser moderator, bool? state)
    {
        trigger.IsActive = state ?? !trigger.IsActive;
        trigger.Action   = new ModerationAction(moderator);

        await _db.SaveChangesAsync();
    }

    public Task UpdateReprimandAsync(Reprimand reprimand, ReprimandDetails details,
        CancellationToken cancellationToken = default)
        => UpdateReprimandAsync(reprimand, details, ReprimandStatus.Updated, cancellationToken);

    public async Task<Ban?> TryUnbanAsync(ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var activeBan = await _db.BanHistory
            .FirstOrDefaultAsync(m => m.IsActive()
                    && m.UserId == details.User.Id
                    && m.GuildId == details.Guild.Id,
                cancellationToken);

        if (activeBan is null)
            await details.Guild.RemoveBanAsync(details.User);
        else
            await ExpireBanAsync(activeBan, cancellationToken);

        return activeBan;
    }

    public async Task<Mute?> TryUnmuteAsync(ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var activeMute = await _db.MuteHistory
            .FirstOrDefaultAsync(m => m.IsActive()
                    && m.UserId == details.User.Id
                    && m.GuildId == details.Guild.Id,
                cancellationToken);

        if (activeMute is not null) await ExpireReprimandAsync(activeMute, cancellationToken);
        await EndMuteAsync(await details.GetUserAsync());

        return activeMute;
    }

    public async Task<ReprimandResult?> TryBanAsync(uint? deleteDays, TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = details.User;
            var days = deleteDays ?? 1;

            var ban = _db.Add(new Ban(days, length, details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            var result = await PublishReprimandAsync(ban, details, cancellationToken);
            await details.Guild.AddBanAsync(user, (int) days, details.Reason);

            return result;
        }
        catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<ReprimandResult?> TryKickAsync(ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await details.GetUserAsync();
            if (user is null) return null;

            await user.KickAsync(details.Reason);

            var kick = _db.Add(new Kick(details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            return await PublishReprimandAsync(kick, details, cancellationToken);
        }
        catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<ReprimandResult?> TryMuteAsync(TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var activeMute = await _db.MuteHistory
            .FirstOrDefaultAsync(m => m.IsActive()
                    && m.UserId == details.User.Id
                    && m.GuildId == details.Guild.Id,
                cancellationToken);

        var user = await details.GetUserAsync();
        if (user is null) return null;

        var guildEntity = await _db.Guilds.TrackGuildAsync(user.Guild, cancellationToken);

        var muteRole = guildEntity.ModerationRules.MuteRoleId;
        if (muteRole is null) return null;

        try
        {
            await user.AddRoleAsync(muteRole.Value);
            if (user.VoiceChannel is not null)
                await user.ModifyAsync(u => u.Mute = true);

            if (activeMute is not null)
            {
                if (!guildEntity.ModerationRules.ReplaceMutes)
                    return null;

                await ExpireReprimandAsync(activeMute, cancellationToken);
            }

            var mute = _db.Add(new Mute(length, details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            return await PublishReprimandAsync(mute, details, cancellationToken);
        }
        catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<ReprimandResult> NoteAsync(ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var note = _db.Add(new Note(details)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        return await PublishReprimandAsync(note, details, cancellationToken);
    }

    public async Task<ReprimandResult> NoticeAsync(ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var guild = await details.GetGuildAsync(_db, cancellationToken);
        var notice = new Notice(guild.ModerationRules.NoticeExpiryLength, details);

        _db.Add(notice);
        await _db.SaveChangesAsync(cancellationToken);

        return await PublishReprimandAsync(notice, details, cancellationToken);
    }

    public async Task<ReprimandResult> WarnAsync(uint amount, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var guild = await details.GetGuildAsync(_db, cancellationToken);
        var warning = new Warning(amount, guild.ModerationRules.WarningExpiryLength, details);

        _db.Add(warning);
        await _db.SaveChangesAsync(cancellationToken);

        return await PublishReprimandAsync(warning, details, cancellationToken);
    }

    protected override async Task OnExpiredEntity(ExpirableReprimand reprimand,
        CancellationToken cancellationToken)
    {
        await (reprimand switch
        {
            Ban ban   => ExpireBanAsync(ban, cancellationToken),
            Mute mute => ExpireMuteAsync(mute, cancellationToken),
            _         => ExpireReprimandAsync(reprimand, cancellationToken)
        });
    }

    private static bool TryGetTriggerSource(Reprimand reprimand, out TriggerSource? source)
    {
        source = reprimand switch
        {
            Censored => TriggerSource.Censored,
            Notice   => TriggerSource.Notice,
            Warning  => TriggerSource.Warning,
            _        => null
        };

        return source is not null;
    }

    private async Task EndMuteAsync(IGuildUser? user)
    {
        if (user is null) return;
        var guildEntity = await _db.Guilds.TrackGuildAsync(user.Guild);

        if (guildEntity.ModerationRules.MuteRoleId is not null)
            await user.RemoveRoleAsync(guildEntity.ModerationRules.MuteRoleId.Value);

        if (user.VoiceChannel is not null)
            await user.ModifyAsync(u => u.Mute = false);
    }

    private async Task ExpireBanAsync(ExpirableReprimand ban, CancellationToken cancellationToken)
    {
        await ExpireReprimandAsync(ban, cancellationToken);

        var guild = _client.GetGuild(ban.GuildId);
        _ = guild.RemoveBanAsync(ban.UserId);
    }

    private async Task ExpireMuteAsync(ExpirableReprimand mute, CancellationToken cancellationToken)
    {
        await ExpireReprimandAsync(mute, cancellationToken);

        var guild = _client.GetGuild(mute.GuildId);
        var user = guild.GetUser(mute.UserId);

        _ = EndMuteAsync(user);
    }

    private async Task ExpireReprimandAsync(ExpirableReprimand reprimand, CancellationToken cancellationToken)
    {
        var guild = _client.GetGuild(reprimand.GuildId);
        var user = await _client.Rest.GetUserAsync(reprimand.UserId);
        var moderator = guild.CurrentUser;
        var details = new ReprimandDetails(user, moderator, "[Reprimand Expired]");

        reprimand.EndedAt  = DateTimeOffset.UtcNow;
        reprimand.ExpireAt = DateTimeOffset.UtcNow;
        await UpdateReprimandAsync(reprimand, details, ReprimandStatus.Expired, cancellationToken);
    }

    private async Task UpdateReprimandAsync(Reprimand reprimand, ReprimandDetails details,
        ReprimandStatus status, CancellationToken cancellationToken)
    {
        reprimand.Status         = status;
        reprimand.ModifiedAction = details;

        await _db.SaveChangesAsync(cancellationToken);
        await _logging.PublishReprimandAsync(reprimand, details, cancellationToken);
    }

    private async Task<ReprimandResult?> ReprimandAsync(ReprimandAction? reprimand, ReprimandDetails details,
        CancellationToken cancellationToken) => reprimand switch
    {
        IBan b     => await TryBanAsync(b.DeleteDays, b.Length, details, cancellationToken),
        IKick      => await TryKickAsync(details, cancellationToken),
        IMute m    => await TryMuteAsync(m.Length, details, cancellationToken),
        IWarning w => await WarnAsync(w.Count, details, cancellationToken),
        INotice    => await NoticeAsync(details, cancellationToken),
        INote      => await NoteAsync(details, cancellationToken),
        _          => null
    };

    private async Task<ReprimandResult> PublishReprimandAsync<T>(T reprimand, ReprimandDetails details,
        CancellationToken cancellationToken) where T : Reprimand
    {
        if (reprimand is ExpirableReprimand expirable)
            EnqueueExpirableEntity(expirable, cancellationToken);

        var result = await TriggerReprimand(details, reprimand, cancellationToken);
        return await _logging.PublishReprimandAsync(result, details, cancellationToken);
    }

    private async Task<ReprimandResult> TriggerReprimand<T>(ReprimandDetails details, T reprimand,
        CancellationToken cancellationToken) where T : Reprimand
    {
        if (!TryGetTriggerSource(reprimand, out var source))
            return reprimand;

        var count = await reprimand.CountAsync(_db, false, cancellationToken);
        var trigger = await GetCountTriggerAsync(reprimand, count, source!.Value, cancellationToken);
        if (trigger is null) return new ReprimandResult(reprimand);

        var (_, moderator, _, _) = details;
        var currentUser = await moderator.Guild.GetCurrentUserAsync();

        var countDetails = new ReprimandDetails(
            details.User, currentUser, $"[Reprimand Count Triggered] at {count}", trigger);
        var secondary = await ReprimandAsync(trigger.Reprimand, countDetails, cancellationToken);

        return new ReprimandResult(reprimand, secondary);
    }

    private async Task<ReprimandTrigger?> GetCountTriggerAsync(Reprimand reprimand, uint count,
        TriggerSource source, CancellationToken cancellationToken)
    {
        var user = await reprimand.GetUserAsync(_db, cancellationToken);
        var rules = user.Guild.ModerationRules;

        return rules.Triggers
            .OfType<ReprimandTrigger>()
            .Where(t => t.IsActive)
            .Where(t => t.Source == source)
            .Where(t => t.IsTriggered(count))
            .OrderByDescending(t => t.Amount)
            .FirstOrDefault();
    }
}