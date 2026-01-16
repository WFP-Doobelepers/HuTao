using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
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
public class ModerationModule(
    AuthorizationService auth,
    CommandErrorHandler error,
    ModerationService moderation,
    HuTaoContext db)
    : ModuleBase<SocketCommandContext>
{
    [Command("ban")]
    [Summary("Ban a user from the current guild.")]
    public async Task BanAsync(
        [RequireHigherRole] IUser user,
        uint deleteDays = 0, TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.Ban)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        if (deleteDays > 7)
        {
            await error.AssociateError(Context.Message, "Failed to ban user. Delete Days cannot be greater than 7.");
            return;
        }

        var details = await GetDetailsAsync(user, reason, category);
        var result = await moderation.TryBanAsync(deleteDays, length, details);

        if (result is null)
            await error.AssociateError(Context.Message, "Failed to ban user.");
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("ban")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public Task BanAsync(
        [RequireHigherRole] IUser user, uint deleteDays = 0,
        [Remainder] string? reason = null)
        => BanAsync(user, deleteDays, null, null, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("ban")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public Task BanAsync([RequireHigherRole] IUser user, [Remainder] string? reason = null)
        => BanAsync(user, 0, null, null, reason);

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("ban")]
    public async Task BanAsync(
        [RequireHigherRole] IEnumerable<IUser> users,
        uint deleteDays = 0, TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.Ban)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await BanAsync(user, deleteDays, length, category, reason);
        }
    }

    [Priority(-4)]
    [HiddenFromHelp]
    [Command("ban")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public async Task BanAsync(
        [RequireHigherRole] IEnumerable<IUser> users,
        uint deleteDays = 0,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await BanAsync(user, deleteDays, null, null, reason);
        }
    }

    [Priority(-5)]
    [HiddenFromHelp]
    [Command("ban")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public async Task BanAsync(
        [RequireHigherRole] IEnumerable<IUser> users,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await BanAsync(user, 0, null, null, reason);
        }
    }

    [Command("hardmute")]
    [Summary("Hard Mute a user from the current guild.")]
    public async Task HardMuteAsync(
        [RequireHigherRole] IGuildUser user,
        TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.HardMute)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await moderation.TryHardMuteAsync(length, details);

        if (result is null)
        {
            await error.AssociateError(Context.Message, "Failed to mute user. " +
                "Either the user is already muted or there is no hard mute role configured. " +
                "Configure the mute role by running the 'configure hard mute' command.");
        }
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("hardmute")]
    [RequireAuthorization(AuthorizationScope.HardMute)]
    public Task HardMuteAsync(
        [RequireHigherRole] IGuildUser user,
        TimeSpan? length = null, [Remainder] string? reason = null)
        => HardMuteAsync(user, length, null, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("hardmute")]
    [RequireAuthorization(AuthorizationScope.HardMute)]
    public Task HardMuteAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => HardMuteAsync(user, null, null, reason);

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("hardmute")]
    public async Task HardMuteAsync(
        [RequireHigherRole] IEnumerable<IGuildUser> users, TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.HardMute)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await HardMuteAsync(user, length, category, reason);
        }
    }

    [Priority(-4)]
    [HiddenFromHelp]
    [Command("hardmute")]
    [RequireAuthorization(AuthorizationScope.HardMute)]
    public async Task HardMuteAsync(
        [RequireHigherRole] IEnumerable<IGuildUser> users,
        TimeSpan? length = null, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await HardMuteAsync(user, length, null, reason);
        }
    }

    [Priority(-5)]
    [HiddenFromHelp]
    [Command("hardmute")]
    [RequireAuthorization(AuthorizationScope.HardMute)]
    public async Task HardMuteAsync(
        [RequireHigherRole] IEnumerable<IGuildUser> users,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await HardMuteAsync(user, null, null, reason);
        }
    }

    [Command("kick")]
    [Summary("Kick a user from the current guild.")]
    public async Task KickAsync(
        [RequireHigherRole] IGuildUser user,
        [CheckCategory(AuthorizationScope.Kick)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await moderation.TryKickAsync(details);

        if (result is null)
            await error.AssociateError(Context.Message, "Failed to kick user.");
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("kick")]
    [RequireAuthorization(AuthorizationScope.Kick)]
    public Task KickAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => KickAsync(user, null, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("kick")]
    public async Task KickAsync(
        [RequireHigherRole] IEnumerable<IGuildUser> users,
        [CheckCategory(AuthorizationScope.Kick)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await KickAsync(user, category, reason);
        }
    }

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("kick")]
    [RequireAuthorization(AuthorizationScope.Kick)]
    public async Task KickAsync([RequireHigherRole] IEnumerable<IGuildUser> users, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await KickAsync(user, null, reason);
        }
    }

    [Command("mute")]
    [Summary("Mute a user from the current guild.")]
    public async Task MuteAsync(
        [RequireHigherRole] IGuildUser user,
        TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.Mute)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await moderation.TryMuteAsync(length, details);

        if (result is null)
        {
            await error.AssociateError(Context.Message, "Failed to mute user. " +
                "Either the user is already muted or there is no mute role configured. " +
                "Configure the mute role by running the 'configure mute' command.");
        }
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("mute")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task MuteAsync(
        [RequireHigherRole] IGuildUser user,
        TimeSpan? length = null, [Remainder] string? reason = null)
        => MuteAsync(user, length, null, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("mute")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task MuteAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => MuteAsync(user, null, null, reason);

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("mute")]
    public async Task MuteAsync(
        [RequireHigherRole] IEnumerable<IGuildUser> users, TimeSpan? length = null,
        [CheckCategory(AuthorizationScope.Mute)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await MuteAsync(user, length, category, reason);
        }
    }

    [Priority(-4)]
    [HiddenFromHelp]
    [Command("mute")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public async Task MuteAsync(
        [RequireHigherRole] IEnumerable<IGuildUser> users,
        TimeSpan? length = null, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await MuteAsync(user, length, null, reason);
        }
    }

    [Priority(-5)]
    [HiddenFromHelp]
    [Command("mute")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public async Task MuteAsync(
        [RequireHigherRole] IEnumerable<IGuildUser> users,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await MuteAsync(user, null, null, reason);
        }
    }

    [Priority(1)]
    [Command("mutes")]
    [Alias("mute list", "mutelist")]
    [Summary("View active mutes on the current guild.")]
    [RequireAuthorization(AuthorizationScope.History)]
    public Task MuteListAsync(ModerationCategory? category = null)
        => moderation.SendMuteListAsync(Context, category, false);

    [Command("note")]
    [Summary("Add a note to a user. Notes are always silent.")]
    public async Task NoteAsync(
        [RequireHigherRole] IUser user,
        [CheckCategory(AuthorizationScope.Note)]
        ModerationCategory? category = null,
        [Remainder] string? note = null)
    {
        var details = await GetDetailsAsync(user, note, category);
        await moderation.NoteAsync(details);
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("note")]
    [RequireAuthorization(AuthorizationScope.Note)]
    public Task NoteAsync([RequireHigherRole] IUser user, [Remainder] string? note = null)
        => NoteAsync(user, null, note);

    [Priority(-2)]
    [Command("note")]
    [Summary("Add a note to several users. Comma separated.")]
    public async Task NoteAsync(
        [RequireHigherRole] IEnumerable<IUser> users,
        [CheckCategory(AuthorizationScope.Note)]
        ModerationCategory? category = null,
        [Remainder] string? note = null)
    {
        foreach (var user in users)
        {
            await NoteAsync(user, category, note);
        }
    }

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("note")]
    [RequireAuthorization(AuthorizationScope.Note)]
    public async Task NoteAsync([RequireHigherRole] IEnumerable<IUser> users, [Remainder] string? note = null)
    {
        foreach (var user in users)
        {
            await NoteAsync(user, null, note);
        }
    }

    [Command("notice")]
    [Summary("Add a notice to a user. This counts as a minor warning.")]
    public async Task NoticeAsync(
        [RequireHigherRole] IGuildUser user,
        [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        await moderation.NoticeAsync(details);
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("notice")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public Task NoticeAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => NoticeAsync(user, null, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("notice")]
    public async Task NoticeAsync(
        [RequireHigherRole] IEnumerable<IGuildUser> users,
        [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await NoticeAsync(user, category, reason);
        }
    }

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("notice")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public async Task NoticeAsync([RequireHigherRole] IEnumerable<IGuildUser> users, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await NoticeAsync(user, null, reason);
        }
    }

    [Command("say")]
    [Summary("Make the bot send a message to the specified channel")]
    [RequireAuthorization(AuthorizationScope.Send)]
    public Task SayAsync(ITextChannel? channel, [Remainder] string message)
        => moderation.SendMessageAsync(Context, channel, message);

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
    [Command("template")]
    [Alias("t")]
    [Summary("Run a configured moderation template")]
    public async Task TemplateAsync(string name, [RequireHigherRole] params IUser[] users)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var template = guild.ModerationTemplates
            .FirstOrDefault(t => name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));

        if (template is null)
        {
            var components = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay("## Moderation Template\nNo template found with this name.")
                    .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay("-# Use the templates list to see available names.")
                    .WithAccentColor(0x9B59FF))
                .WithActionRow(new ActionRowBuilder()
                    .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary))
                .Build();

            await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
            return;
        }

        if (!await auth.IsAuthorizedAsync(Context, template.Scope))
        {
            await error.AssociateError(Context.Message, "You don't have permission to use this template.");
            return;
        }

        var failed = new List<IUser>();
        foreach (var user in users.DistinctBy(u => u.Id))
        {
            var details = await GetDetailsAsync(user, template.Reason, null);
            var result = await moderation.ReprimandAsync(template, details);

            if (result is null) failed.Add(user);
        }

        if (failed.Count > 0)
        {
            var components = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay($"## Moderation Template\nFailed to run template on {failed.Count} {failed.Humanize()}.")
                    .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay("-# Check permissions, role hierarchy, and the template scope.")
                    .WithAccentColor(0x9B59FF))
                .WithActionRow(new ActionRowBuilder()
                    .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary))
                .Build();

            await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
        }
    }

    [Command("timeout")]
    [Summary("Timeout a user from the current guild.")]
    public async Task TimeoutAsync(
        [RequireHigherRole] IGuildUser user,
        TimeSpan length,
        [CheckCategory(AuthorizationScope.Timeout)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await moderation.TryTimeoutAsync(length, details);

        if (result is null)
            await error.AssociateError(Context.Message, "Failed to timeout user.");
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("timeout")]
    [RequireAuthorization(AuthorizationScope.Timeout)]
    public Task TimeoutAsync(
        [RequireHigherRole] IGuildUser user,
        TimeSpan length,
        [Remainder] string? reason = null)
        => TimeoutAsync(user, length, null, reason);

    [Command("untimeout")]
    [Summary("Remove timeout from a user in the current guild.")]
    public async Task UntimeoutAsync(
        IGuildUser user,
        [CheckCategory(AuthorizationScope.Timeout)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await moderation.TryUntimeoutAsync(details);

        if (!result)
            await error.AssociateError(Context.Message, "Failed to remove timeout from user.");
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("untimeout")]
    [RequireAuthorization(AuthorizationScope.Timeout)]
    public Task UntimeoutAsync(
        IGuildUser user,
        [Remainder] string? reason = null)
        => UntimeoutAsync(user, null, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("untimeout")]
    public async Task UntimeoutAsync(
        IEnumerable<IGuildUser> users,
        [CheckCategory(AuthorizationScope.Timeout)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await UntimeoutAsync(user, category, reason);
        }
    }

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("untimeout")]
    [RequireAuthorization(AuthorizationScope.Timeout)]
    public async Task UntimeoutAsync(IEnumerable<IGuildUser> users, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await UntimeoutAsync(user, null, reason);
        }
    }

    [Command("unban")]
    [Summary("Unban a user from the current guild.")]
    public async Task UnbanAsync(
        IUser user,
        [CheckCategory(AuthorizationScope.Ban)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await moderation.TryUnbanAsync(details);

        if (result is null)
            await error.AssociateError(Context.Message, "This user has no ban logs. Forced unban.");
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("unban")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public Task UnbanAsync(IUser user, [Remainder] string? reason = null) => UnbanAsync(user, null, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("unban")]
    public async Task UnbanAsync(
        IEnumerable<IUser> users,
        [CheckCategory(AuthorizationScope.Ban)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await UnbanAsync(user, category, reason);
        }
    }

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("unban")]
    [RequireAuthorization(AuthorizationScope.Ban)]
    public async Task UnbanAsync(IEnumerable<IUser> users, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await UnbanAsync(user, null, reason);
        }
    }

    [Command("unmute")]
    [Summary("Unmute a user from the current guild.")]
    public async Task UnmuteAsync(
        IGuildUser user,
        [CheckCategory(AuthorizationScope.Mute)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        var result = await moderation.TryUnmuteAsync(details);

        if (!result)
            await error.AssociateError(Context.Message, "Unmute failed.");
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("unmute")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public Task UnmuteAsync(IGuildUser user, [Remainder] string? reason = null) => UnmuteAsync(user, null, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("unmute")]
    public async Task UnmuteAsync(
        IEnumerable<IGuildUser> users,
        [CheckCategory(AuthorizationScope.Mute)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await UnmuteAsync(user, category, reason);
        }
    }

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("unmute")]
    [RequireAuthorization(AuthorizationScope.Mute)]
    public async Task UnmuteAsync(IEnumerable<IGuildUser> users, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await UnmuteAsync(user, null, reason);
        }
    }

    [Command("warn")]
    [Summary("Warn a user from the current guild.")]
    public async Task WarnAsync(
        [RequireHigherRole] IGuildUser user,
        [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        uint amount = 1, [Remainder] string? reason = null)
    {
        var details = await GetDetailsAsync(user, reason, category);
        await moderation.WarnAsync(amount, details);
    }

    [Priority(-1)]
    [HiddenFromHelp]
    [Command("warn")]
    public Task WarnAsync(
        [RequireHigherRole] IGuildUser user, uint amount = 1,
        [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
        => WarnAsync(user, category, amount, reason);

    [Priority(-2)]
    [HiddenFromHelp]
    [Command("warn")]
    public Task WarnAsync(
        [RequireHigherRole] IGuildUser user,
        [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
        => WarnAsync(user, category, 1, reason);

    [Priority(-3)]
    [HiddenFromHelp]
    [Command("warn")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public Task WarnAsync([RequireHigherRole] IGuildUser user, uint amount = 1, [Remainder] string? reason = null)
        => WarnAsync(user, null, amount, reason);

    [Priority(-4)]
    [HiddenFromHelp]
    [Command("warn")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public Task WarnAsync([RequireHigherRole] IGuildUser user, [Remainder] string? reason = null)
        => WarnAsync(user, null, 1, reason);

    [Priority(-5)]
    [HiddenFromHelp]
    [Command("warn")]
    public async Task WarnAsync(
        IEnumerable<IGuildUser> users, uint amount = 1,
        [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await WarnAsync(user, category, amount, reason);
        }
    }

    [Priority(-6)]
    [HiddenFromHelp]
    [Command("warn")]
    public async Task WarnAsync(
        IEnumerable<IGuildUser> users,
        [CheckCategory(AuthorizationScope.Warning)]
        ModerationCategory? category = null,
        [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await WarnAsync(user, category, 1, reason);
        }
    }

    [Priority(-7)]
    [HiddenFromHelp]
    [Command("warn")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public async Task WarnAsync(IEnumerable<IGuildUser> users, uint amount = 1, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await WarnAsync(user, null, amount, reason);
        }
    }

    [Priority(-8)]
    [HiddenFromHelp]
    [Command("warn")]
    [RequireAuthorization(AuthorizationScope.Warning)]
    public async Task WarnAsync(IEnumerable<IGuildUser> users, [Remainder] string? reason = null)
    {
        foreach (var user in users)
        {
            await WarnAsync(user, null, 1, reason);
        }
    }

    private async Task<ReprimandDetails> GetDetailsAsync(
        IUser user, string? reason, ModerationCategory? category)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var variables = guild.ModerationRules?.Variables;
        var details = new ReprimandDetails(Context, user, reason, variables, category: category);

        await db.Users.TrackUserAsync(details);
        await db.SaveChangesAsync();

        return details;
    }
}