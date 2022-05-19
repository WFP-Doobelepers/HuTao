using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[Name("Moderation")]
[Summary("Guild moderation commands.")]
[RequireContext(ContextType.Guild)]
public class ModerationModule : ModuleBase<SocketCommandContext>
{
    private readonly AuthorizationService _auth;
    private readonly CommandErrorHandler _error;
    private readonly HuTaoContext _db;
    private readonly ModerationService _moderation;

    public ModerationModule(
        AuthorizationService auth, CommandErrorHandler error,
        ModerationService moderation, HuTaoContext db)
    {
        _auth       = auth;
        _error      = error;
        _moderation = moderation;
        _db         = db;
    }

    [Command("ban")]
    [Summary("Ban a user from the current guild.")]
    public async Task BanAsync(
        [RequireHigherRole] IUser user,
        uint deleteDays = 0, TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.Ban)] ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        if (deleteDays > 7)
        {
            await _error.AssociateError(Context.Message, "Failed to ban user. Delete Days cannot be greater than 7.");
            return;
        }

        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryBanAsync(deleteDays, length, details);

        if (result is null)
            await _error.AssociateError(Context.Message, "Failed to ban user.");
    }

    [Priority(-2)]
    [Command("ban")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public Task BanAsync([RequireHigherRole] IUser user, [Remainder] string? reason = null)
        => BanAsync(user, 0, null, null, reason);

    [Priority(-1)]
    [Command("ban")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public Task BanAsync(
        [RequireHigherRole] IUser user, uint deleteDays = 0,
        [Remainder] string? reason = null)
        => BanAsync(user, deleteDays, null, null, reason);

    [Priority(-1)]
    [Command("mute")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task HardMuteAsync([RequireHigherRole] IGuildUser user,
        TimeSpan? length = null, [Remainder] string? reason = null)
        => MuteAsync(user, length, null, reason);

    [Command("hardmute")]
    [Summary("Hard Mute a user from the current guild.")]
    public async Task HardMuteAsync([RequireHigherRole] IGuildUser user,
        TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.HardMute)] ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryHardMuteAsync(length, details);

        if (result is null)
        {
            await _error.AssociateError(Context.Message, "Failed to mute user. " +
                "Either the user is already muted or there is no hard mute role configured. " +
                "Configure the mute role by running the 'configure hard mute' command.");
        }
    }

    [Priority(-2)]
    [Command("hardmute")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task HardMuteAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => HardMuteAsync(user, null, null, reason);

    [Command("kick")]
    [Summary("Kick a user from the current guild.")]
    public async Task KickAsync([RequireHigherRole] IGuildUser user,
        [CheckCategory(AuthorizationScope.Kick)] ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryKickAsync(details);

        if (result is null)
            await _error.AssociateError(Context.Message, "Failed to kick user.");
    }

    [Priority(-1)]
    [Command("kick")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Kick)]
    public Task KickAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => KickAsync(user, null, reason);

    [Command("mute")]
    [Summary("Mute a user from the current guild.")]
    public async Task MuteAsync([RequireHigherRole] IGuildUser user,
        TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.Mute)] ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryMuteAsync(length, details);

        if (result is null)
        {
            await _error.AssociateError(Context.Message, "Failed to mute user. " +
                "Either the user is already muted or there is no mute role configured. " +
                "Configure the mute role by running the 'configure mute' command.");
        }
    }

    [Priority(-2)]
    [Command("mute")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task MuteAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => MuteAsync(user, null, null, reason);

    [Priority(-1)]
    [Command("hardmute")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task MuteAsync([RequireHigherRole] IGuildUser user,
        TimeSpan? length = null, [Remainder] string? reason = null)
        => HardMuteAsync(user, length, null, reason);

    [Priority(1)]
    [Command("mutes")]
    [Alias("mute list", "mutelist")]
    [Summary("View active mutes on the current guild.")]
    [RequireAuthorization(AuthorizationScope.History)]
    public Task MuteListAsync(ModerationCategory? category = null)
        => _moderation.SendMuteListAsync(Context, category, false);

    [Command("note")]
    [Summary("Add a note to a user. Notes are always silent.")]
    public async Task NoteAsync([RequireHigherRole] IUser user,
        [CheckCategory(AuthorizationScope.Note)] ModerationCategory? category = null,
        [Remainder] string? note = null)
    {
        var details = await GetDetailsAsync(user, note, category);
        await _moderation.NoteAsync(details);
    }

    [Priority(-1)]
    [Command("note")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task NoteAsync([RequireHigherRole] IUser user, [Remainder] string? note = null)
        => NoteAsync(user, null, note);

    [Command("notice")]
    [Summary("Add a notice to a user. This counts as a minor warning.")]
    public async Task NoticeAsync([RequireHigherRole] IGuildUser user,
        [CheckCategory(AuthorizationScope.Warning)] ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        await _moderation.NoticeAsync(details);
    }

    [Priority(-1)]
    [Command("notice")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public Task NoticeAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => NoticeAsync(user, null, reason);

    [Command("say")]
    [Summary("Make the bot send a message to the specified channel")]
    [RequireAuthorization(AuthorizationScope.Send)]
    public Task SayAsync(ITextChannel? channel, [Remainder] string message)
        => _moderation.SendMessageAsync(Context, channel, message);

    [Command("say")]
    [HiddenFromHelp]
    [Summary("Make the bot send a message to the specified channel")]
    [RequireAuthorization(AuthorizationScope.Send)]
    public Task SayAsync([Remainder] string message) => SayAsync(null, message);

    [Command("slowmode")]
    [Summary("Set a slowmode in the channel.")]
    [RequireBotPermission(ChannelPermission.ManageChannels)]
    [RequireUserPermission(ChannelPermission.ManageChannels, Group = nameof(AuthorizationScope.Slowmode))]
    [RequireAuthorization(AuthorizationScope.Slowmode, Group = nameof(AuthorizationScope.Slowmode))]
    public Task SlowmodeAsync(TimeSpan? length = null, ITextChannel? channel = null)
        => length is null && channel is null
            ? ModerationService.ShowSlowmodeChannelsAsync(Context)
            : ModerationService.SlowmodeChannelAsync(Context, length, channel);

    [HiddenFromHelp]
    [Command("slowmode")]
    [Summary("Set a slowmode in the channel.")]
    [RequireBotPermission(ChannelPermission.ManageChannels)]
    [RequireUserPermission(ChannelPermission.ManageChannels, Group = nameof(AuthorizationScope.Slowmode))]
    [RequireAuthorization(AuthorizationScope.Slowmode, Group = nameof(AuthorizationScope.Slowmode))]
    public Task SlowmodeAsync(ITextChannel? channel = null, TimeSpan? length = null) => SlowmodeAsync(length, channel);

    [Priority(-1)]
    [Command("template")] [Alias("t")]
    [Summary("Run a configured moderation template")]
    public async Task TemplateAsync(string name, [RequireHigherRole] IUser user)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var template = guild.ModerationTemplates
            .FirstOrDefault(t => name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));

        if (template is null)
        {
            await ReplyAsync("No template found with this name.");
            return;
        }

        if (!await _auth.IsAuthorizedAsync(Context, template.Scope))
        {
            await _error.AssociateError(Context.Message, "You don't have permission to use this template.");
            return;
        }

        var details = await GetDetailsAsync(user, template.Reason, null);
        var result = await _moderation.ReprimandAsync(template, details);

        if (result is null)
            await _error.AssociateError(Context.Message, "Failed to use the template.");
    }

    [Command("unban")]
    [Summary("Unban a user from the current guild.")]
    public async Task UnbanAsync(IUser user,
        [CheckCategory(AuthorizationScope.Ban)] ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryUnbanAsync(details);

        if (result is null)
            await _error.AssociateError(Context.Message, "This user has no ban logs. Forced unban.");
    }

    [Priority(-1)]
    [Command("unban")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public Task UnbanAsync(IUser user, [Remainder] string? reason = null) => UnbanAsync(user, null, reason);

    [Command("unmute")]
    [Summary("Unmute a user from the current guild.")]
    public async Task UnmuteAsync(IGuildUser user,
        [CheckCategory(AuthorizationScope.Mute)] ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryUnmuteAsync(details);

        if (!result)
            await _error.AssociateError(Context.Message, "Unmute failed.");
    }

    [Priority(-1)]
    [Command("unmute")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task UnmuteAsync(IGuildUser user, [Remainder] string? reason = null) => UnmuteAsync(user, null, reason);

    [Command("warn")]
    [Summary("Warn a user from the current guild.")]
    public async Task WarnAsync([RequireHigherRole] IGuildUser user,
        [CheckCategory(AuthorizationScope.Warning)] ModerationCategory? category = null,
        uint amount = 1, [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        await _moderation.WarnAsync(amount, details);
    }

    [Priority(-1)]
    [Command("warn")]
    [HiddenFromHelp]
    public Task WarnAsync([RequireHigherRole] IGuildUser user, uint amount = 1,
        [CheckCategory(AuthorizationScope.Warning)] ModerationCategory? category = null,
        [Remainder] string? reason = null)
        => WarnAsync(user, category, amount, reason);

    [Priority(-2)]
    [Command("warn")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public Task WarnAsync([RequireHigherRole] IGuildUser user, uint amount = 1, [Remainder] string? reason = null)
        => WarnAsync(user, null, amount, reason);

    [Priority(-3)]
    [Command("warn")]
    [HiddenFromHelp]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public Task WarnAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => WarnAsync(user, null, 1, reason);

    private async Task<ReprimandDetails> GetDetailsAsync(
        IUser user, string? reason, ModerationCategory? category)
    {
        var details = new ReprimandDetails(Context, user, reason, category: category);

        await _db.Users.TrackUserAsync(details);
        await _db.SaveChangesAsync();

        return details;
    }
}