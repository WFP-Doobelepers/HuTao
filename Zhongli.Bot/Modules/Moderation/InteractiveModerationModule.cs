using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Autocomplete;
using Zhongli.Services.Core.Preconditions.Interactions;
using Zhongli.Services.Interactive.Paginator;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation;

public class InteractiveModerationModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AuthorizationService _auth;
    private readonly InteractiveService _interactive;
    private readonly ModerationService _moderation;
    private readonly ZhongliContext _db;

    public InteractiveModerationModule(
        AuthorizationService auth, InteractiveService interactive,
        ModerationService moderation, ZhongliContext db)
    {
        _auth        = auth;
        _interactive = interactive;
        _moderation  = moderation;
        _db          = db;
    }

    [SlashCommand("ban", "Ban a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public async Task BanAsync(
        [RequireHigherRole] IUser user,
        uint deleteDays = 0, TimeSpan? length = null,
        string? reason = null, bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        if (deleteDays > 7)
        {
            await FollowupAsync("Failed to ban user. Delete Days cannot be greater than 7.");
            return;
        }

        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryBanAsync(deleteDays, length, details);

        if (result is null)
            await FollowupAsync("Failed to ban user.");
    }

    [SlashCommand("kick", "Kick a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Kick)]
    public async Task KickAsync([RequireHigherRole] IGuildUser user,
        string? reason = null, bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryKickAsync(details);

        if (result is null)
            await FollowupAsync("Failed to kick user.");
    }

    [SlashCommand("mute", "Mute a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public async Task MuteAsync([RequireHigherRole] IGuildUser user,
        TimeSpan? length = null,
        string? reason = null, bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryMuteAsync(length, details);

        if (result is null)
        {
            await FollowupAsync("Failed to mute user. " +
                "Either the user is already muted or there is no mute role configured. " +
                "Configure the mute role by running the 'configure mute' command.");
        }
    }

    [SlashCommand("mutelist", "View active mutes on the current guild.")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public async Task MuteListAsync(bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var history = guild.ReprimandHistory.OfType<Mute>()
            .Where(r => r.IsActive())
            .Where(r => r.Status
                is not ReprimandStatus.Expired
                or ReprimandStatus.Pardoned
                or ReprimandStatus.Deleted);

        var embed = new EmbedBuilder()
            .WithTitle("Currently Active Mutes");

        var pages = history
            .OrderByDescending(r => r.Action?.Date)
            .Select(CreateEmbed)
            .ToPageBuilders(EmbedBuilder.MaxFieldCount, embed);

        var paginator = InteractiveExtensions.CreateDefaultPaginator().WithPages(pages);
        await _interactive.SendPaginatorAsync(
            paginator.WithUsers(Context.User).Build(), Context.Interaction,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    [SlashCommand("note", "Add a note to a user. Notes are always silent.")]
    [RequireAuthorization(AuthorizationScope.Note)]
    public async Task NoteAsync([RequireHigherRole] IGuildUser user, string? note = null, bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, note);
        await _moderation.NoteAsync(details);
    }

    [SlashCommand("notice", "Add a notice to a user. This counts as a minor warning.")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public async Task NoticeAsync(
        [RequireHigherRole] IGuildUser user,
        string? reason = null,
        bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason);
        await _moderation.NoticeAsync(details);
    }

    [SlashCommand("say", "Make the bot send a message to the specified channel")]
    [RequireAuthorization(AuthorizationScope.Helper)]
    public async Task SayAsync(string message, ITextChannel? channel = null)
    {
        channel ??= (ITextChannel) Context.Channel;
        await channel.SendMessageAsync(message, allowedMentions: AllowedMentions.None);
        await FollowupAsync("Message sent.", ephemeral: channel.Id == Context.Channel.Id);
    }

    [SlashCommand("slowmode", "Set a slowmode in the channel.")]
    [RequireAuthorization(AuthorizationScope.Helper)]
    [RequireBotPermission(ChannelPermission.ManageChannels)]
    public Task SlowmodeAsync(TimeSpan? length = null, ITextChannel? channel = null)
        => length is null && channel is null
            ? ModerationService.ShowSlowmodeChannelsAsync(Context)
            : ModerationService.SlowmodeChannelAsync(Context, length, channel);

    [SlashCommand("template", "Run a configured moderation template")]
    public async Task TemplateAsync([Autocomplete(typeof(ModerationAutocomplete))] string name,
        [RequireHigherRole] IUser user, bool ephemeral = false)
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

        var details = await GetDetailsAsync(user, template.Reason);
        var result = await _moderation.ReprimandAsync(template, details);

        if (result is null)
            await FollowupAsync("Failed to use the template.");
    }

    [SlashCommand("unban", "Unban a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public async Task UnbanAsync(IUser user, string? reason = null, bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryUnbanAsync(details);

        if (result is null)
            await FollowupAsync("This user has no ban logs. Forced unban.");
    }

    [SlashCommand("unmute", "Unmute a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public async Task UnmuteAsync(IGuildUser user, string? reason = null, bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryUnmuteAsync(details);

        if (result is null)
            await FollowupAsync("Unmute failed.");
    }

    [SlashCommand("warn", "Warn a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public async Task WarnAsync([RequireHigherRole] IGuildUser user,
        uint amount = 1, string? reason = null, bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var details = await GetDetailsAsync(user, reason);
        await _moderation.WarnAsync(amount, details);
    }

    private static EmbedFieldBuilder CreateEmbed(Reprimand r) => new EmbedFieldBuilder()
        .WithName(r.GetTitle(true))
        .WithValue(r.GetReprimandExpiration());

    private async Task<ReprimandDetails> GetDetailsAsync(IUser user, string? reason)
    {
        var details = new ReprimandDetails(user, Context, reason);

        await _db.Users.TrackUserAsync(details);
        await _db.SaveChangesAsync();

        return details;
    }
}