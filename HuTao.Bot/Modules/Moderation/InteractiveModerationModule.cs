using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[RequireContext(ContextType.Guild)]
public class InteractiveModerationModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AuthorizationService _auth;
    private readonly HuTaoContext _db;
    private readonly ModerationService _moderation;

    public InteractiveModerationModule(AuthorizationService auth, ModerationService moderation, HuTaoContext db)
    {
        _auth       = auth;
        _moderation = moderation;
        _db         = db;
    }

    [SlashCommand("ban", "Ban a user from the current guild.")]
    public async Task BanAsync(
        [RequireHigherRole] IUser user, uint deleteDays = 0,
        TimeSpan? length = null, string? reason = null,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(AuthorizationScope.Ban)]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        if (deleteDays > 7)
        {
            await FollowupAsync("Failed to ban user. Delete Days cannot be greater than 7.");
            return;
        }

        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryBanAsync(deleteDays, length, details);

        if (result is null)
            await FollowupAsync("Failed to ban user.");
    }

    [SlashCommand("kick", "Kick a user from the current guild.")]
    public async Task KickAsync(
        [RequireHigherRole] IGuildUser user, string? reason = null,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(AuthorizationScope.Kick)]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryKickAsync(details);

        if (result is null)
            await FollowupAsync("Failed to kick user.");
    }

    [SlashCommand("mute", "Mute a user from the current guild.")]
    public async Task MuteAsync([RequireHigherRole] IGuildUser user,
        TimeSpan? length = null, string? reason = null,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(AuthorizationScope.Mute)]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryMuteAsync(length, details);

        if (result is null)
        {
            await FollowupAsync("Failed to mute user. " +
                "Either the user is already muted or there is no mute role configured. " +
                "Configure the mute role by running the 'configure mute' command.");
        }
    }

    [SlashCommand("mutelist", "View active mutes on the current guild.")]
    [RequireAuthorization(AuthorizationScope.History)]
    public Task MuteListAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
        => _moderation.SendMuteListAsync(Context, category, ephemeral);

    [SlashCommand("note", "Add a note to a user. Notes are always silent.")]
    public async Task NoteAsync(
        [RequireHigherRole] IGuildUser user, string? note = null,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(AuthorizationScope.Note)]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, note, category);
        await _moderation.NoteAsync(details);
    }

    [SlashCommand("notice", "Add a notice to a user. This counts as a minor warning.")]
    public async Task NoticeAsync(
        [RequireHigherRole] IGuildUser user, string? reason = null,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason, category);
        await _moderation.NoticeAsync(details);
    }

    [SlashCommand("say", "Make the bot send a message to the specified channel")]
    [RequireAuthorization(AuthorizationScope.Send)]
    public Task SayAsync(string message, ITextChannel? channel = null)
        => _moderation.SendMessageAsync(Context, channel, message);

    [SlashCommand("slowmode", "Set a slowmode in the channel.")]
    [RequireBotPermission(ChannelPermission.ManageChannels)]
    [RequireUserPermission(ChannelPermission.ManageChannels, Group = nameof(AuthorizationScope.Slowmode))]
    [RequireAuthorization(AuthorizationScope.Slowmode, Group = nameof(AuthorizationScope.Slowmode))]
    public Task SlowmodeAsync(TimeSpan? length = null, ITextChannel? channel = null)
        => length is null && channel is null
            ? ModerationService.ShowSlowmodeChannelsAsync(Context)
            : ModerationService.SlowmodeChannelAsync(Context, length, channel);

    [SlashCommand("template", "Run a configured moderation template")]
    public async Task TemplateAsync(
        [Autocomplete(typeof(TemplateAutocomplete))] string name,
        [RequireHigherRole] IUser user,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var template = guild.ModerationTemplates
            .FirstOrDefault(t => name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));

        if (template is null)
        {
            await FollowupAsync("No template found with this name.");
            return;
        }

        if (!await _auth.IsAuthorizedAsync(Context, template.Scope))
        {
            await FollowupAsync("You don't have permission to use this template.");
            return;
        }

        var details = await GetDetailsAsync(user, template.Reason, template.Category);
        var result = await _moderation.ReprimandAsync(template, details);

        if (result is null)
            await FollowupAsync("Failed to use the template.");
    }

    [SlashCommand("unban", "Unban a user from the current guild.")]
    public async Task UnbanAsync(
        IUser user, string? reason = null,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(AuthorizationScope.Ban)]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryUnbanAsync(details);

        if (result is null)
            await FollowupAsync("This user has no ban logs. Forced unban.");
    }

    [SlashCommand("unmute", "Unmute a user from the current guild.")]
    public async Task UnmuteAsync(
        IGuildUser user, string? reason = null,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(AuthorizationScope.Mute)]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason, category);
        var result = await _moderation.TryUnmuteAsync(details);

        if (result is null)
            await FollowupAsync("Unmute failed.");
    }

    [SlashCommand("warn", "Warn a user from the current guild.")]
    public async Task WarnAsync(
        [RequireHigherRole] IGuildUser user, uint amount = 1, string? reason = null,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason, category);
        await _moderation.WarnAsync(amount, details);
    }

    private async Task<ReprimandDetails> GetDetailsAsync(
        IUser user, string? reason, ModerationCategory? category)
    {
        var details = new ReprimandDetails(Context, user, reason, category: category);

        await _db.Users.TrackUserAsync(details);
        await _db.SaveChangesAsync();

        return details;
    }
}