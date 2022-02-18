using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Interactive.Paginator;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation;

[Name("Moderation")]
[Summary("Guild moderation commands.")]
public class ModerationModule : ModuleBase<SocketCommandContext>
{
    private readonly AuthorizationService _auth;
    private readonly CommandErrorHandler _error;
    private readonly InteractiveService _interactive;
    private readonly ModerationService _moderation;
    private readonly ZhongliContext _db;

    public ModerationModule(AuthorizationService auth, CommandErrorHandler error,
        InteractiveService interactive,
        ModerationService moderation,
        ZhongliContext db)
    {
        _auth        = auth;
        _error       = error;
        _interactive = interactive;
        _moderation  = moderation;
        _db          = db;
    }

    [Command("ban")]
    [Summary("Ban a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public async Task BanAsync([RequireHigherRole] IUser user, uint deleteDays = 0,
        TimeSpan? length = null,
        [Remainder] string? reason = null)
    {
        if (deleteDays > 7)
        {
            await _error.AssociateError(Context.Message, "Failed to ban user. Delete Days cannot be greater than 7.");
            return;
        }

        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryBanAsync(deleteDays, length, details);

        if (result is null)
            await _error.AssociateError(Context.Message, "Failed to ban user.");
    }

    [Command("ban")]
    [HiddenFromHelp]
    [Summary("Ban a user permanently from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public Task BanAsync([RequireHigherRole] IUser user, [Remainder] string? reason = null)
        => BanAsync(user, 0, null, reason);

    [Command("ban")]
    [HiddenFromHelp]
    [Summary("Ban a user permanently from the current guild, and delete messages.")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public Task BanAsync([RequireHigherRole] IUser user, uint deleteDays = 0,
        [Remainder] string? reason = null)
        => BanAsync(user, deleteDays, null, reason);

    [Command("kick")]
    [Summary("Kick a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Kick)]
    public async Task KickAsync([RequireHigherRole] IGuildUser user,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryKickAsync(details);

        if (result is null)
            await _error.AssociateError(Context.Message, "Failed to kick user.");
    }

    [Command("mute")]
    [Summary("Mute a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public async Task MuteAsync([RequireHigherRole] IGuildUser user,
        TimeSpan? length = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryMuteAsync(length, details);

        if (result is null)
        {
            await _error.AssociateError(Context.Message, "Failed to mute user. " +
                "Either the user is already muted or there is no mute role configured. " +
                "Configure the mute role by running the 'configure mute' command.");
        }
    }

    [Command("mute list")]
    [Summary("View active mutes on the current guild.")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public async Task MuteListAsync()
    {
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
            .ToPageBuilder(embed);

        var paginator = InteractiveExtensions.CreateDefaultPaginator().WithPages(pages);

        await _interactive.SendPaginatorAsync(paginator.WithUsers(Context.User).Build(), Context.Channel);
    }

    [Command("note")]
    [Summary("Add a note to a user. Notes are always silent.")]
    [RequireAuthorization(AuthorizationScope.Note)]
    public async Task NoteAsync([RequireHigherRole] IGuildUser user,
        [Remainder] string? note = null)
    {
        var details = await GetDetailsAsync(user, note);
        await _moderation.NoteAsync(details);

        await Context.Message.DeleteAsync();
    }

    [Command("notice")]
    [Summary("Add a notice to a user. This counts as a minor warning.")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public async Task NoticeAsync([RequireHigherRole] IGuildUser user,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason);
        await _moderation.NoticeAsync(details);
    }

    [Command("say")]
    [Summary("Make the bot send a message to the specified channel")]
    [RequireAuthorization(AuthorizationScope.Helper)]
    public async Task SayAsync(ITextChannel? channel, [Remainder] string message)
    {
        channel ??= (ITextChannel) Context.Channel;
        await Context.Message.DeleteAsync();
        await channel.SendMessageAsync(message, allowedMentions: AllowedMentions.None);
    }

    [Command("say")]
    [HiddenFromHelp]
    [Summary("Make the bot send a message to the specified channel")]
    [RequireAuthorization(AuthorizationScope.Helper)]
    public Task SayAsync([Remainder] string message) => SayAsync(null, message);

    [Command("slowmode")]
    [Summary("Set a slowmode in the channel.")]
    [RequireBotPermission(ChannelPermission.ManageChannels)]
    [RequireAuthorization(AuthorizationScope.Helper)]
    public async Task SlowmodeAsync(TimeSpan? length = null, ITextChannel? channel = null)
    {
        if (length is null && channel is null)
        {
            var channels = Context.Guild.Channels.OfType<ITextChannel>()
                .Where(c => c is not INewsChannel)
                .Where(c => c.SlowModeInterval is not 0)
                .OrderBy(c => c.Position);

            var embed = new EmbedBuilder()
                .WithTitle("List of channels with slowmode active")
                .AddItemsIntoFields("Channels", channels,
                    c => $"{c.Mention} => {c.SlowModeInterval.Seconds().Humanize()}")
                .WithColor(Color.Green)
                .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

            await ReplyAsync(embed: embed.Build());
        }
        else
        {
            length  ??= TimeSpan.Zero;
            channel ??= (ITextChannel) Context.Channel;
            var seconds = (int) length.Value.TotalSeconds;
            await channel.ModifyAsync(c => c.SlowModeInterval = seconds);

            if (seconds is 0)
                await ReplyAsync($"Slowmode disabled for {channel.Mention}");
            else
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Slowmode enabled")
                    .AddField("Channel", channel.Mention, true)
                    .AddField("Delay", length.Value.Humanize(3), true)
                    .WithColor(Color.Green)
                    .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

                await ReplyAsync(embed: embed.Build());
            }
        }
    }

    [Command("slowmode")]
    [HiddenFromHelp]
    [Summary("Set a slowmode in the channel.")]
    [RequireBotPermission(ChannelPermission.ManageChannels)]
    [RequireAuthorization(AuthorizationScope.Helper)]
    public Task SlowmodeAsync(ITextChannel? channel = null, TimeSpan? length = null)
        => SlowmodeAsync(length, channel);

    [Command("template")]
    [Alias("t")]
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

        var details = await GetDetailsAsync(user, template.Reason);
        var result = await _moderation.ReprimandAsync(template, details);

        if (result is null)
            await _error.AssociateError(Context.Message, "Failed to use the template.");
    }

    [Command("unban")]
    [Summary("Unban a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public async Task UnbanAsync(IUser user, [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryUnbanAsync(details);

        if (result is null)
            await _error.AssociateError(Context.Message, "This user has no ban logs. Forced unban.");
    }

    [Command("unmute")]
    [Summary("Unmute a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public async Task UnmuteAsync(IGuildUser user, [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason);
        var result = await _moderation.TryUnmuteAsync(details);

        if (result is null)
            await _error.AssociateError(Context.Message, "Unmute failed.");
    }

    [Command("warn")]
    [Summary("Warn a user from the current guild.")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public async Task WarnAsync([RequireHigherRole] IGuildUser user, uint amount = 1,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason);
        await _moderation.WarnAsync(amount, details);
    }

    [Command("warn")]
    [Summary("Warn a user from the current guild once.")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public Task WarnAsync([RequireHigherRole] IGuildUser user,
        [Remainder] string? reason = null)
        => WarnAsync(user, 1, reason);

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