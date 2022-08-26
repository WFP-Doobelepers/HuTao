using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Fergun.Interactive;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.Core;
using HuTao.Services.Expirable;
using HuTao.Services.Interactive;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Moderation;

public class ModerationService : ExpirableService<ExpirableReprimand>
{
    private readonly AuthorizationService _auth;
    private readonly DiscordSocketClient _client;
    private readonly HuTaoContext _db;
    private readonly InteractiveService _interactive;
    private readonly ModerationLoggingService _logging;

    public ModerationService(
        IMemoryCache cache, HuTaoContext db,
        AuthorizationService auth, DiscordSocketClient client,
        InteractiveService interactive, ModerationLoggingService logging) : base(cache, db)
    {
        _auth        = auth;
        _client      = client;
        _db          = db;
        _interactive = interactive;
        _logging     = logging;
    }

    public async Task ConfigureHardMuteRoleAsync(IModerationRules rules, IGuild guild, IRole? role,
        bool skipPermissions)
    {
        role ??= guild.Roles.FirstOrDefault(r => r.Id == rules.MuteRoleId);
        role ??= guild.Roles.FirstOrDefault(r => r.Id == rules.HardMuteRoleId);
        role ??= guild.Roles.FirstOrDefault(r => r.Name == "Hard Muted");
        role ??= await guild.CreateRoleAsync("Hard Muted", isMentionable: false);

        rules.HardMuteRoleId = role.Id;
        await _db.SaveChangesAsync();

        if (skipPermissions) return;
        var permissions = new OverwritePermissions(
            addReactions: PermValue.Deny,
            sendMessages: PermValue.Deny,
            speak: PermValue.Deny,
            stream: PermValue.Deny);

        var channels = await guild.GetChannelsAsync();
        foreach (var channel in channels.Where(channel => channel is not IThreadChannel))
        {
            await channel.AddPermissionOverwriteAsync(role, permissions);
        }
    }

    public async Task ConfigureMuteRoleAsync(IModerationRules rules, IGuild guild, IRole? role, bool skipPermissions)
    {
        role ??= guild.Roles.FirstOrDefault(r => r.Id == rules.MuteRoleId);
        role ??= guild.Roles.FirstOrDefault(r => r.Name == "Muted");
        role ??= await guild.CreateRoleAsync("Muted", isMentionable: false);

        rules.MuteRoleId = role.Id;
        await _db.SaveChangesAsync();

        if (skipPermissions) return;
        var permissions = new OverwritePermissions(
            addReactions: PermValue.Deny,
            sendMessages: PermValue.Deny,
            speak: PermValue.Deny,
            stream: PermValue.Deny);

        var channels = await guild.GetChannelsAsync();
        foreach (var channel in channels.Where(channel => channel is not IThreadChannel))
        {
            await channel.AddPermissionOverwriteAsync(role, permissions);
        }
    }

    public async Task DeleteReprimandAsync(Reprimand reprimand, ReprimandDetails? details,
        CancellationToken cancellationToken = default)
    {
        if (reprimand is ExpirableReprimand expirable)
            await OnExpiredEntity(expirable, cancellationToken);

        if (details is not null)
            await UpdateReprimandAsync(reprimand, ReprimandStatus.Deleted, details, cancellationToken);

        if (reprimand is Filtered filtered)
            _db.RemoveRange(filtered.Messages);

        if (reprimand.Action is not null)
            _db.Remove(reprimand.Action);

        if (reprimand.ModifiedAction is not null)
            _db.Remove(reprimand.ModifiedAction);

        _db.Remove(reprimand);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTriggerAsync(Trigger trigger, IGuildUser moderator, bool silent)
    {
        var reprimands = await _db.Set<Reprimand>().Where(r => r.TriggerId == trigger.Id).ToListAsync();

        foreach (var reprimand in reprimands)
        {
            var details = await GetModified(reprimand);
            await DeleteReprimandAsync(reprimand, silent ? null : details);

            async Task<ReprimandDetails> GetModified(IUserEntity r)
            {
                var user = await _client.Rest.GetUserAsync(r.UserId);
                return new ReprimandDetails(user, moderator, "[Deleted Trigger]");
            }
        }

        _db.Remove(trigger);
        _db.TryRemove(trigger.Action);

        if (trigger is ReprimandTrigger rep)
        {
            _db.TryRemove(rep.Reprimand);
            if (rep.Reprimand is RoleAction role)
                _db.RemoveRange(role.Roles);
        }

        await _db.SaveChangesAsync();
    }

    public async Task SendMessageAsync(Context context, ITextChannel? channel, string message)
    {
        var ephemeral = await _auth.IsAuthorizedAsync(context, AuthorizationScope.All | AuthorizationScope.Ephemeral);
        channel ??= (ITextChannel) context.Channel;
        if (context.User is not IGuildUser user)
            await context.ReplyAsync("You must be a guild user to use this command.");
        else if (!user.GetPermissions(channel).SendMessages)
            await context.ReplyAsync("You do not have permission to send messages in this channel.");
        else
        {
            await channel.SendMessageAsync(message, allowedMentions: AllowedMentions.None);
            await context.ReplyAsync($"Message sent to {channel.Mention}",
                ephemeral: ephemeral && channel.Id == context.Channel.Id);
        }
    }

    public async Task SendMuteListAsync(Context context, ModerationCategory? category, bool ephemeral)
    {
        await context.DeferAsync(ephemeral);
        var guild = await _db.Guilds.TrackGuildAsync(context.Guild);
        var history = guild.ReprimandHistory.OfType<Mute>()
            .Where(r => r.IsActive())
            .Where(r => r.Status
                is not ReprimandStatus.Expired
                or ReprimandStatus.Pardoned
                or ReprimandStatus.Deleted)
            .Where(r => category is null || r.Category?.Id == category.Id);

        var builders = history
            .OrderByDescending(r => r.Action?.Date)
            .Select(r => new EmbedBuilder().WithExpirableDetails(r))
            .Chunk(10);

        var pages = builders.Select(b => new MultiEmbedPageBuilder().WithBuilders(b));
        var paginator = InteractiveExtensions.CreateDefaultPaginator().WithPages(pages).Build();

        await (context switch
        {
            CommandContext command => _interactive.SendPaginatorAsync(paginator, command.Channel),

            InteractionContext { Interaction: SocketInteraction interaction }
                => _interactive.SendPaginatorAsync(paginator, interaction, ephemeral: ephemeral,
                    responseType: InteractionResponseType.DeferredChannelMessageWithSource),

            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Invalid context.")
        });
    }

    public static async Task ShowSlowmodeChannelsAsync(Context context)
    {
        var textChannels = await context.Guild.GetTextChannelsAsync();
        var channels = textChannels
            .Where(c => c is not INewsChannel)
            .Where(c => c.SlowModeInterval is not 0)
            .OrderBy(c => c.Position);

        var embed = new EmbedBuilder()
            .WithTitle("List of channels with slowmode active")
            .AddItemsIntoFields("Channels", channels,
                c => $"{c.Mention} => {c.SlowModeInterval.Seconds().Humanize()}")
            .WithColor(Color.Green)
            .WithUserAsAuthor(context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await context.ReplyAsync(embed: embed.Build(), ephemeral: true);
    }

    public static async Task SlowmodeChannelAsync(Context context, TimeSpan? length, ITextChannel? channel)
    {
        length  ??= TimeSpan.Zero;
        channel ??= (ITextChannel) context.Channel;
        var seconds = (int) length.Value.TotalSeconds;
        await channel.ModifyAsync(c => c.SlowModeInterval = seconds);

        if (seconds is 0)
            await context.ReplyAsync($"Slowmode disabled for {channel.Mention}", ephemeral: true);
        else
        {
            var embed = new EmbedBuilder()
                .WithTitle("Slowmode enabled")
                .AddField("Channel", channel.Mention, true)
                .AddField("Delay", length.Value.Humanize(3), true)
                .WithColor(Color.Green)
                .WithUserAsAuthor(context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

            await context.ReplyAsync(embed: embed.Build(), ephemeral: true);
        }
    }

    public async Task ToggleTriggerAsync(Trigger trigger, IGuildUser moderator, bool? state)
    {
        trigger.IsActive = state ?? !trigger.IsActive;
        trigger.Action   = new ModerationAction(moderator);

        await _db.SaveChangesAsync();
    }

    public async Task TryExpireReprimandAsync(Reprimand reprimand, ReprimandStatus status,
        ReprimandDetails? details = null, CancellationToken cancellationToken = default)
        => await (reprimand switch
        {
            Ban ban              => ExpireBanAsync(ban, status, cancellationToken, details),
            Mute mute            => ExpireMuteAsync(mute, status, cancellationToken, details),
            RoleReprimand role   => ExpireRolesAsync(role, status, cancellationToken, details),
            ExpirableReprimand e => ExpireReprimandAsync(e, status, cancellationToken, details),
            _ => throw new ArgumentOutOfRangeException(
                nameof(reprimand), reprimand, "Reprimand is not expirable.")
        });

    public Task UpdateReprimandAsync(Reprimand reprimand, ReprimandDetails details,
        CancellationToken cancellationToken = default)
        => UpdateReprimandAsync(reprimand, ReprimandStatus.Updated, details, cancellationToken);

    public async Task<Ban?> TryUnbanAsync(ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var activeBan = await _db.GetActive<Ban>(details, cancellationToken);
        if (activeBan is not null)
            await ExpireBanAsync(activeBan, ReprimandStatus.Pardoned, cancellationToken, details);
        else
            await details.Guild.RemoveBanAsync(details.User);

        return activeBan;
    }

    public async Task<bool> TryUnmuteAsync(ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var mute = await _db.GetActive<Mute>(details, cancellationToken);
        return mute is not null
            ? await ExpireMuteAsync(mute, ReprimandStatus.Pardoned, cancellationToken, details)
            : await EndMuteAsync(await details.GetUserAsync(), null, details.Category);
    }

    public async Task<ReprimandResult?> AutoReprimandAsync(
        IReadOnlyCollection<IUserMessage> messages, TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var logs = messages.Select(m => new MessageDeleteLog(details.Guild, m, details)).ToList();
        var filtered = _db.Add(new Filtered(logs, length, details)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        if (details.Trigger is AutoConfiguration config)
        {
            if (config.DeleteMessages) _ = DeleteMessagesAsync();

            var result = await ReprimandAsync(config.Reprimand, details, cancellationToken);
            if (result is not null) return result;
        }
        else _ = DeleteMessagesAsync();
        return await PublishReprimandAsync(filtered, details, cancellationToken);

        async Task DeleteMessagesAsync()
        {
            var options = new RequestOptions { CancelToken = cancellationToken };
            foreach (var group in messages.GroupBy(m => m.Channel))
            {
                try
                {
                    if (group.Key is not SocketTextChannel channel) continue;
                    await channel.DeleteMessagesAsync(group, options);
                }
                catch (HttpException)
                {
                    // Ignored
                }
            }
        }
    }

    public async Task<ReprimandResult?> CensorAsync(
        SocketMessage message, TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var censored = _db.Add(new Censored(message.Content, length, details)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        if (details.Trigger is Censor censor)
        {
            if (!censor.Silent) _ = message.DeleteAsync();

            var reprimand = await CensorReprimandAsync(details, censored, censor, cancellationToken);
            if (reprimand is not null) return reprimand;
        }
        else _ = message.DeleteAsync();

        return await PublishReprimandAsync(censored, details, cancellationToken);
    }

    public async Task<ReprimandResult?> CensorNameAsync(
        string name, string replace, TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var user = await details.GetUserAsync();
        if (user is null) return null;

        await user.ModifyAsync(u => u.Nickname = replace);
        var censored = _db.Add(new Censored(name, length, details)).Entity;
        await _db.SaveChangesAsync(cancellationToken);

        if (details.Trigger is Censor censor)
        {
            var result = await ReprimandAsync(censor.Reprimand, details, cancellationToken);
            if (result is not null) return result;
        }

        return await PublishReprimandAsync(censored, details, cancellationToken);
    }

    public Task<ReprimandResult?> ReprimandAsync(ModerationTemplate template, ReprimandDetails details,
        CancellationToken cancellationToken = default)
        => ReprimandAsync(template.Action, details, cancellationToken);

    public async Task<ReprimandResult?> TryBanAsync(uint? deleteDays, TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var activeBan = await _db.GetActive<Ban>(details, cancellationToken);
        if (activeBan is not null)
            await ExpireReprimandAsync(activeBan, ReprimandStatus.Pardoned, cancellationToken, details);

        try
        {
            var user = details.User;
            var days = deleteDays ?? 1;

            var ban = _db.Add(new Ban(days, length, details)).Entity;

            var result = await PublishReprimandAsync(ban, details, cancellationToken);
            await details.Guild.AddBanAsync(user, (int) days, $"{details.Moderator}: {details.Reason}".Truncate(512));

            await _db.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<ReprimandResult?> TryHardMuteAsync(TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var user = await details.GetUserAsync();
        if (user is null) return null;

        var guildEntity = await _db.Guilds.TrackGuildAsync(user.Guild, cancellationToken);
        var activeMute = await _db.GetActive<HardMute>(details, cancellationToken);

        var muteRole = details.Category?.HardMuteRoleId
            ?? details.Category?.MuteRoleId
            ?? guildEntity.ModerationRules?.HardMuteRoleId
            ?? guildEntity.ModerationRules?.MuteRoleId;

        if (muteRole is null) return null;

        if (activeMute is not null)
        {
            var replace = details.Category?.ReplaceMutes ?? guildEntity.ModerationRules?.ReplaceMutes ?? false;
            if (!replace) return null;

            await ExpireReprimandAsync(activeMute, ReprimandStatus.Pardoned, cancellationToken, details);
        }

        try
        {
            await user.AddRoleAsync(muteRole.Value);

            var removed = new List<RoleEntity>();
            foreach (var id in user.RoleIds.Where(r => r != user.Guild.EveryoneRole.Id && r != muteRole.Value))
            {
                try
                {
                    await user.RemoveRoleAsync(id, new RequestOptions { CancelToken = cancellationToken });
                    removed.Add(await _db.Roles.TrackRoleAsync(user.Guild.Id, id, cancellationToken));
                }
                catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
                {
                    // Ignore
                }
            }

            var mute = _db.Add(new HardMute(length, removed, details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            return await PublishReprimandAsync(mute, details, cancellationToken);
        }
        catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
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

            var kick = _db.Add(new Kick(details)).Entity;

            var result = await PublishReprimandAsync(kick, details, cancellationToken);
            await user.KickAsync(details.Reason);

            await _db.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<ReprimandResult?> TryMuteAsync(TimeSpan? length, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var user = await details.GetUserAsync();
        if (user is null) return null;

        var guildEntity = await _db.Guilds.TrackGuildAsync(user.Guild, cancellationToken);
        var activeMute = await _db.GetActive<Mute>(details, cancellationToken);

        var muteRole = details.Category?.MuteRoleId ?? guildEntity.ModerationRules?.MuteRoleId;
        if (muteRole is null) return null;

        if (activeMute is not null)
        {
            var replace = details.Category?.ReplaceMutes ?? guildEntity.ModerationRules?.ReplaceMutes ?? false;
            if (!replace) return null;

            await ExpireReprimandAsync(activeMute, ReprimandStatus.Pardoned, cancellationToken, details);
        }

        try
        {
            await user.AddRoleAsync(muteRole.Value);
            if (user.VoiceChannel is not null)
                await user.ModifyAsync(u => u.Mute = true);

            var mute = _db.Add(new Mute(length, details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            return await PublishReprimandAsync(mute, details, cancellationToken);
        }
        catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
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
        var expiry = details.Category?.NoticeExpiryLength ?? guild.ModerationRules?.NoticeExpiryLength;
        var notice = new Notice(expiry, details);

        _db.Add(notice);
        await _db.SaveChangesAsync(cancellationToken);

        return await PublishReprimandAsync(notice, details, cancellationToken);
    }

    public async Task<ReprimandResult> WarnAsync(uint amount, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var guild = await details.GetGuildAsync(_db, cancellationToken);
        var expiry = details.Category?.WarningExpiryLength ?? guild.ModerationRules?.WarningExpiryLength;
        var warning = new Warning(amount, expiry, details);

        _db.Add(warning);
        await _db.SaveChangesAsync(cancellationToken);

        return await PublishReprimandAsync(warning, details, cancellationToken);
    }

    protected override Task OnExpiredEntity(ExpirableReprimand reprimand, CancellationToken cancellationToken)
        => TryExpireReprimandAsync(reprimand, ReprimandStatus.Expired, cancellationToken: cancellationToken);

    private static bool TryGetTriggerSource(Reprimand reprimand, [NotNullWhen(true)] out TriggerSource? source)
    {
        source = reprimand switch
        {
            Censored => TriggerSource.Censored,
            Notice   => TriggerSource.Notice,
            Warning  => TriggerSource.Warning,
            Filtered => TriggerSource.Filtered,
            _        => null
        };

        return source is not null;
    }

    private async Task ExpireBanAsync(ExpirableReprimand ban, ReprimandStatus status,
        CancellationToken cancellationToken, ReprimandDetails? details = null)
    {
        var guild = _client.GetGuild(ban.GuildId);
        _ = guild.RemoveBanAsync(ban.UserId);

        await ExpireReprimandAsync(ban, status, cancellationToken, details);
    }

    private async Task ExpireReprimandAsync(ExpirableReprimand reprimand, ReprimandStatus status,
        CancellationToken cancellationToken, ReprimandDetails? details = null)
    {
        if (details is null)
        {
            var guild = _client.GetGuild(reprimand.GuildId);
            var user = await _client.Rest.GetUserAsync(reprimand.UserId);

            details = new ReprimandDetails(user, guild.CurrentUser, $"[Reprimand {status}]");
        }

        reprimand.EndedAt ??= DateTimeOffset.UtcNow;
        await UpdateReprimandAsync(reprimand, status, details, cancellationToken);
    }

    private async Task ExpireRolesAsync(RoleReprimand roles, ReprimandStatus status,
        CancellationToken cancellationToken, ReprimandDetails? details = null)
    {
        var guild = (IGuild) _client.GetGuild(roles.GuildId);
        var user = await guild.GetUserAsync(roles.UserId);

        var templates = roles.Roles.Select(role =>
        {
            return role.Modify(r => r.Behavior = r.Behavior switch
            {
                RoleBehavior.Add    => RoleBehavior.Remove,
                RoleBehavior.Remove => RoleBehavior.Add,
                _                   => role.Behavior
            });
        });

        await user.AddRolesAsync(templates.ToList(), cancellationToken);
        await ExpireReprimandAsync(roles, status, cancellationToken, details);
    }

    private async Task UpdateReprimandAsync(Reprimand reprimand,
        ReprimandStatus status, ReprimandDetails details,
        CancellationToken cancellationToken)
    {
        reprimand.Status         = status;
        reprimand.ModifiedAction = details;

        await _db.SaveChangesAsync(cancellationToken);
        await _logging.PublishReprimandAsync(reprimand, details, cancellationToken);
    }

    private async Task<bool> EndMuteAsync(IGuildUser? user, ILength? mute, IModerationRules? rules)
    {
        if (user is null) return false;

        var guild = await _db.Guilds.TrackGuildAsync(user.Guild);
        var roleId = rules?.MuteRoleId ?? guild.ModerationRules?.MuteRoleId;

        if (mute is HardMute hard)
        {
            roleId = rules?.HardMuteRoleId ?? guild.ModerationRules?.HardMuteRoleId ?? roleId;
            foreach (var role in hard.Roles)
            {
                try
                {
                    await user.AddRoleAsync(role.RoleId);
                }
                catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
                {
                    // Ignore
                }
            }
        }

        if (roleId is not null)
        {
            if (user.HasRole(roleId.Value))
                await user.RemoveRoleAsync(roleId.Value);
            else
                return false;
        }

        if (user.VoiceChannel is not null)
            await user.ModifyAsync(u => u.Mute = false);

        return true;
    }

    private async Task<bool> ExpireMuteAsync(Mute mute, ReprimandStatus status,
        CancellationToken cancellationToken, ReprimandDetails? details = null)
    {
        var guild = (IGuild) _client.GetGuild(mute.GuildId);
        var user = await guild.GetUserAsync(mute.UserId);

        var result = await EndMuteAsync(user, mute, mute.Category);
        await ExpireReprimandAsync(mute, status, cancellationToken, details);

        return result;
    }

    private async Task<ReprimandResult?> CensorReprimandAsync(ReprimandDetails details,
        Censored censored, Censor censor, CancellationToken cancellationToken)
    {
        var count = await censored.CountAsync(censor, _db, cancellationToken);
        if (!censor.IsTriggered((uint) count.Active)) return null;

        return await ReprimandAsync(censor.Reprimand, details with
        {
            Reason = $"[Reprimand Triggered] at {count.Active}",
            Trigger = censor,
            Result = new ReprimandResult(censored)
        }, cancellationToken);
    }

    private async Task<ReprimandResult?> ReprimandAsync(ReprimandAction? reprimand, ReprimandDetails details,
        CancellationToken cancellationToken) => reprimand switch
    {
        BanAction b      => await TryBanAsync(b.DeleteDays, b.Length, details, cancellationToken),
        KickAction       => await TryKickAsync(details, cancellationToken),
        HardMuteAction t => await TryHardMuteAsync(t.Length, details, cancellationToken),
        MuteAction m     => await TryMuteAsync(m.Length, details, cancellationToken),
        RoleAction r     => await TryApplyRolesAsync(r.Roles, r.Length, details, cancellationToken),
        WarningAction w  => await WarnAsync(w.Count, details, cancellationToken),
        NoticeAction     => await NoticeAsync(details, cancellationToken),
        NoteAction       => await NoteAsync(details, cancellationToken),
        _                => null
    };

    private async Task<ReprimandResult?> TryApplyRolesAsync(
        ICollection<RoleTemplate> templates, TimeSpan? length,
        ReprimandDetails details, CancellationToken cancellationToken = default)
    {
        var user = await details.GetUserAsync();
        if (user is null) return null;

        try
        {
            var roles = await user.AddRolesAsync(templates, cancellationToken);

            var all = roles.All.Select(r => r.Template).ToList();
            if (!all.Any()) return null;

            var reprimand = new RoleReprimand(length, all, details);

            var role = _db.Add(reprimand).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            return await PublishReprimandAsync(role, details, cancellationToken);
        }
        catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    private async Task<ReprimandResult> PublishReprimandAsync<T>(T reprimand, ReprimandDetails details,
        CancellationToken cancellationToken) where T : Reprimand
    {
        if (reprimand is ExpirableReprimand expirable)
            EnqueueExpirableEntity(expirable, cancellationToken);

        var result = new ReprimandResult(reprimand, details.Result);
        var secondary = details with { Result = result };

        var trigger = await TryGetTriggerAsync(reprimand, cancellationToken);
        var uniqueTrigger = details.Result?.Secondary.All(r => r.Trigger?.Id != trigger?.Id) ?? true;

        return trigger is not null && uniqueTrigger
            ? await TriggerReprimandAsync(trigger, result, secondary, cancellationToken)
            : await _logging.PublishReprimandAsync(result, secondary, cancellationToken);
    }

    private async Task<ReprimandResult> TriggerReprimandAsync(
        ReprimandTrigger trigger, ReprimandResult result,
        ReprimandDetails details, CancellationToken cancellationToken)
    {
        var reprimand = result.Last;
        var count = await reprimand.CountUserReprimandsAsync(_db, cancellationToken);

        var secondary = await ReprimandAsync(trigger.Reprimand, details with
        {
            Reason = $"[Reprimand Count Triggered] at {count.Active}",
            Trigger = trigger
        }, cancellationToken);

        return secondary is null
            ? await _logging.PublishReprimandAsync(result, details, cancellationToken)
            : new ReprimandResult(secondary.Last, result);
    }

    private async Task<ReprimandTrigger?> GetCountTriggerAsync(Reprimand reprimand, uint count,
        TriggerSource source, CancellationToken cancellationToken)
    {
        var user = await reprimand.GetUserAsync(_db, cancellationToken);
        var rules = user.Guild.ModerationRules;

        return rules?.Triggers
            .OfType<ReprimandTrigger>()
            .Where(t => t.IsActive)
            .Where(t => t.Source == source)
            .Where(t => t.Category?.Id == reprimand.Category?.Id)
            .Where(t => t.IsTriggered(count))
            .MaxBy(t => t.Amount);
    }

    private async Task<ReprimandTrigger?> TryGetTriggerAsync(Reprimand reprimand, CancellationToken cancellationToken)
    {
        if (!TryGetTriggerSource(reprimand, out var source))
            return null;

        var count = await reprimand.CountUserReprimandsAsync(_db, cancellationToken);
        return await GetCountTriggerAsync(reprimand, (uint) count.Active, source.Value, cancellationToken);
    }
}