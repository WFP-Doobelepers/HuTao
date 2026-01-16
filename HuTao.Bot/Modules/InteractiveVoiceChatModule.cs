using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Data;
using HuTao.Data.Models.VoiceChat;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.VoiceChat;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Optional = Discord.Optional;

namespace HuTao.Bot.Modules;

[Group("voice", "Voice chat controls")]
[RequireContext(ContextType.Guild)]
public sealed class InteractiveVoiceChatModule(HuTaoContext db, InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("panel", "Manage your current voice chat with buttons.")]
    public async Task PanelAsync([RequireEphemeralScope] bool ephemeral = false)
        => await OpenAsync(ephemeral);

    [ComponentInteraction(VoiceChatPanelComponentIds.OpenButtonId, true)]
    public async Task OpenFromButtonAsync()
        => await OpenAsync(ephemeral: true);

    [ComponentInteraction(VoiceChatPanelComponentIds.RefreshButtonId, true)]
    public async Task RefreshAsync()
    {
        if (!TryGetPanel(out var paginator, out var state, out _))
            return;

        state.Notice = null;
        RefreshStateFromDiscord(state);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(VoiceChatPanelComponentIds.LockButtonId, true)]
    public async Task ToggleLockAsync()
    {
        if (!TryGetPanel(out var paginator, out var state, out _))
            return;

        var voiceChannel = GetVoiceChannel(state);
        if (voiceChannel is null)
        {
            state.Notice = "Voice channel no longer exists.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (!CanManage(state, voiceChannel, out var reason))
        {
            state.Notice = reason;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        var everyone = voiceChannel.GetPermissionOverwrite(Context.Guild.EveryoneRole);
        if (IsLocked(voiceChannel))
        {
            var overwrite = everyone?.Modify(connect: PermValue.Inherit)
                ?? new OverwritePermissions(connect: PermValue.Inherit);
            await voiceChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        }
        else
        {
            var overwrite = everyone?.Modify(connect: PermValue.Deny)
                ?? new OverwritePermissions(connect: PermValue.Deny);
            await voiceChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        }

        state.Notice = null;
        RefreshStateFromDiscord(state);
        await RenderAsync(paginator);
    }

    [ComponentInteraction(VoiceChatPanelComponentIds.HideButtonId, true)]
    public async Task ToggleHideAsync()
    {
        if (!TryGetPanel(out var paginator, out var state, out _))
            return;

        var voiceChannel = GetVoiceChannel(state);
        if (voiceChannel is null)
        {
            state.Notice = "Voice channel no longer exists.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (!CanManage(state, voiceChannel, out var reason))
        {
            state.Notice = reason;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        var everyone = voiceChannel.GetPermissionOverwrite(Context.Guild.EveryoneRole);
        if (IsHidden(voiceChannel))
        {
            var overwrite = everyone?.Modify(viewChannel: PermValue.Inherit)
                ?? new OverwritePermissions(viewChannel: PermValue.Inherit);
            await voiceChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        }
        else
        {
            var overwrite = everyone?.Modify(viewChannel: PermValue.Deny)
                ?? new OverwritePermissions(viewChannel: PermValue.Deny);
            await voiceChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        }

        state.Notice = null;
        RefreshStateFromDiscord(state);
        await RenderAsync(paginator);
    }

    [ComponentInteraction(VoiceChatPanelComponentIds.LimitButtonId, true)]
    public async Task SetLimitAsync()
    {
        if (!TryGetPanel(out _, out _, out _))
            return;

        await RespondWithModalAsync<VoiceLimitModal>(VoiceChatPanelComponentIds.LimitModalId);
    }

    [ModalInteraction(VoiceChatPanelComponentIds.LimitModalId, true)]
    public async Task SetLimitModalAsync(VoiceLimitModal modal)
    {
        if (!TryGetPanelFromModal(out var paginator, out var state))
        {
            await RespondAsync("This panel is no longer active.", ephemeral: true);
            return;
        }

        var voiceChannel = GetVoiceChannel(state);
        if (voiceChannel is null)
        {
            state.Notice = "Voice channel no longer exists.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (!CanManage(state, voiceChannel, out var reason))
        {
            state.Notice = reason;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        var parsed = ParseLimit(modal.LimitText, out var value, out var error);
        if (!parsed)
        {
            state.Notice = error;
            RefreshStateFromDiscord(state);
            await RenderAsync(paginator);
            return;
        }

        await voiceChannel.ModifyAsync(c => c.UserLimit = new Optional<int?>(value));

        state.Notice = null;
        RefreshStateFromDiscord(state);
        await RenderAsync(paginator);
    }

    [ComponentInteraction(VoiceChatPanelComponentIds.TransferSelectId, true)]
    public async Task TransferAsync(IUser[] users)
    {
        if (!TryGetPanel(out var paginator, out var state, out _))
            return;

        var target = users.FirstOrDefault();
        if (target is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        var voiceChannel = GetVoiceChannel(state);
        var textChannel = GetTextChannel(state);

        if (voiceChannel is null || textChannel is null)
        {
            state.Notice = "Voice/text channel no longer exists.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (!CanManage(state, voiceChannel, out var reason))
        {
            state.Notice = reason;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (target.IsBot || target.IsWebhook)
        {
            state.Notice = "You can't transfer ownership to bots/webhooks.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        var guildTarget = Context.Guild.GetUser(target.Id);
        if (guildTarget is null)
        {
            state.Notice = "That user is not in this guild.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (guildTarget.Id == state.OwnerUserId)
        {
            state.Notice = "That user is already the owner.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        var oldOwner = Context.Guild.GetUser(state.OwnerUserId);
        if (oldOwner is not null)
        {
            await voiceChannel.RemovePermissionOverwriteAsync(oldOwner);
            await textChannel.RemovePermissionOverwriteAsync(oldOwner);
        }

        await voiceChannel.AddPermissionOverwriteAsync(guildTarget, new OverwritePermissions(
            manageChannel: PermValue.Allow,
            muteMembers: PermValue.Allow));

        await textChannel.AddPermissionOverwriteAsync(guildTarget, new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow));

        var link = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == state.TextChannelId);

        if (link is not null)
        {
            link.UserId = guildTarget.Id;
            await db.SaveChangesAsync();
        }

        state.OwnerUserId = guildTarget.Id;
        state.Notice = null;
        RefreshStateFromDiscord(state);
        await RenderAsync(paginator);
    }

    [ComponentInteraction(VoiceChatPanelComponentIds.BanSelectId, true)]
    public async Task BanAsync(IUser[] users)
    {
        if (!TryGetPanel(out var paginator, out var state, out _))
            return;

        var target = users.FirstOrDefault();
        if (target is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (target.Id == Context.User.Id)
        {
            state.Notice = "You can't ban yourself.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        var voiceChannel = GetVoiceChannel(state);
        var textChannel = GetTextChannel(state);

        if (voiceChannel is null || textChannel is null)
        {
            state.Notice = "Voice/text channel no longer exists.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (!CanManage(state, voiceChannel, out var reason))
        {
            state.Notice = reason;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        var guildTarget = Context.Guild.GetUser(target.Id);
        if (guildTarget is null)
        {
            state.Notice = "That user is not in this guild.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        await voiceChannel.AddPermissionOverwriteAsync(guildTarget, OverwritePermissions.DenyAll(voiceChannel));
        await textChannel.AddPermissionOverwriteAsync(guildTarget, OverwritePermissions.DenyAll(textChannel));

        if (guildTarget.VoiceChannel?.Id == voiceChannel.Id)
            await guildTarget.ModifyAsync(u => u.Channel = null);

        state.Notice = null;
        RefreshStateFromDiscord(state);
        await RenderAsync(paginator);
    }

    [ComponentInteraction(VoiceChatPanelComponentIds.UnbanSelectId, true)]
    public async Task UnbanAsync(IUser[] users)
    {
        if (!TryGetPanel(out var paginator, out var state, out _))
            return;

        var target = users.FirstOrDefault();
        if (target is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        var voiceChannel = GetVoiceChannel(state);
        var textChannel = GetTextChannel(state);

        if (voiceChannel is null || textChannel is null)
        {
            state.Notice = "Voice/text channel no longer exists.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (!CanManage(state, voiceChannel, out var reason))
        {
            state.Notice = reason;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        var guildTarget = Context.Guild.GetUser(target.Id);
        if (guildTarget is null)
        {
            state.Notice = "That user is not in this guild.";
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        await voiceChannel.RemovePermissionOverwriteAsync(guildTarget);
        await textChannel.RemovePermissionOverwriteAsync(guildTarget);

        state.Notice = null;
        RefreshStateFromDiscord(state);
        await RenderAsync(paginator);
    }

    private async Task OpenAsync(bool ephemeral)
    {
        await DeferAsync(ephemeral);

        if (Context.Channel is not SocketTextChannel currentText)
        {
            var msg = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay("## Voice Chat Panel\nThis command must be used in a server text channel.")
                    .WithAccentColor(0x9B59FF))
                .Build();
            await FollowupAsync(components: msg, ephemeral: ephemeral, allowedMentions: AllowedMentions.None);
            return;
        }

        var link = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == currentText.Id);

        if (link is null)
        {
            var msg = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay("## Voice Chat Panel\nThis channel isn't linked to a voice chat.")
                    .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay("-# Run this inside a voice chat text channel.")
                    .WithAccentColor(0x9B59FF))
                .Build();

            await FollowupAsync(components: msg, ephemeral: ephemeral, allowedMentions: AllowedMentions.None);
            return;
        }

        var voiceChannel = Context.Guild.GetVoiceChannel(link.VoiceChannelId);
        if (voiceChannel is null)
        {
            var msg = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay("## Voice Chat Panel\nThe linked voice channel no longer exists.")
                    .WithAccentColor(0x9B59FF))
                .Build();

            await FollowupAsync(components: msg, ephemeral: ephemeral, allowedMentions: AllowedMentions.None);
            return;
        }

        var state = VoiceChatPanelState.Create(
            Context.Guild.Name,
            voiceChannel.Id,
            currentText.Id,
            link.UserId);

        state.UpdateStatus(IsLocked(voiceChannel), IsHidden(voiceChannel), voiceChannel.UserLimit);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithUserState(state)
            .WithPageCount(1)
            .WithPageFactory(VoiceChatPanelRenderer.GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: ephemeral,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    private bool TryGetPanel(out IComponentPaginator paginator, out VoiceChatPanelState state, out IComponentInteraction interaction)
    {
        if (Context.Interaction is not IComponentInteraction i
            || !interactive.TryGetComponentPaginator(i.Message, out var p)
            || p is null
            || !p.CanInteract(i.User))
        {
            paginator = null!;
            state = null!;
            interaction = null!;
            return false;
        }

        interaction = i;
        paginator = p;
        state = paginator.GetUserState<VoiceChatPanelState>();
        return true;
    }

    private bool TryGetPanelFromModal(out IComponentPaginator paginator, out VoiceChatPanelState state)
    {
        if (Context.Interaction is not SocketModal modal
            || !interactive.TryGetComponentPaginator(modal.Message, out var p)
            || p is null
            || !p.CanInteract(modal.User))
        {
            paginator = null!;
            state = null!;
            return false;
        }

        paginator = p;
        state = paginator.GetUserState<VoiceChatPanelState>();
        return true;
    }

    private async Task RenderAsync(IComponentPaginator paginator)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(Context.Interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    private SocketVoiceChannel? GetVoiceChannel(VoiceChatPanelState state)
        => Context.Guild.GetVoiceChannel(state.VoiceChannelId);

    private SocketTextChannel? GetTextChannel(VoiceChatPanelState state)
        => Context.Guild.GetTextChannel(state.TextChannelId);

    private bool CanManage(VoiceChatPanelState state, SocketVoiceChannel voiceChannel, out string reason)
    {
        var canManage = Context.User is SocketGuildUser current && current.GetPermissions(voiceChannel).ManageChannel;
        if (state.OwnerUserId == Context.User.Id || canManage)
        {
            reason = string.Empty;
            return true;
        }

        reason = "You must be the owner (or have Manage Channels) to do that.";
        return false;
    }

    private void RefreshStateFromDiscord(VoiceChatPanelState state)
    {
        var voiceChannel = GetVoiceChannel(state);
        if (voiceChannel is null)
        {
            state.UpdateStatus(state.IsLocked, state.IsHidden, state.UserLimit);
            return;
        }

        state.UpdateStatus(IsLocked(voiceChannel), IsHidden(voiceChannel), voiceChannel.UserLimit);
    }

    private static bool IsLocked(SocketVoiceChannel channel)
        => channel.GetPermissionOverwrite(channel.Guild.EveryoneRole)?.Connect is PermValue.Deny;

    private static bool IsHidden(SocketVoiceChannel channel)
        => channel.GetPermissionOverwrite(channel.Guild.EveryoneRole)?.ViewChannel is PermValue.Deny;

    private static bool ParseLimit(string? input, out int? limit, out string error)
    {
        input = input?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            limit = null;
            error = string.Empty;
            return true;
        }

        if (!int.TryParse(input, out var value))
        {
            limit = null;
            error = "Limit must be a number (or empty to clear).";
            return false;
        }

        if (value < 0)
            value = 0;

        if (value > 99)
        {
            limit = null;
            error = "Limit must be ≤ 99.";
            return false;
        }

        limit = value == 0 ? null : value;
        error = string.Empty;
        return true;
    }

    public sealed class VoiceLimitModal : IModal
    {
        public string Title => "Set Voice Limit";

        [InputLabel("User limit (empty = none)")]
        [ModalTextInput(VoiceChatPanelComponentIds.LimitInputId, TextInputStyle.Short, placeholder: "e.g. 2", maxLength: 2)]
        public string? LimitText { get; set; }
    }
}

