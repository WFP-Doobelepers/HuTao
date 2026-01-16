using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Channels;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;

namespace HuTao.Bot.Modules;

[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Channels)]
public sealed class InteractiveChannelsModule(InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("channels", "Browse server channels interactively.")]
    public async Task ChannelsAsync([RequireEphemeralScope] bool ephemeral = false)
        => await OpenAsync(ephemeral);

    [ComponentInteraction(ChannelBrowserComponentIds.OpenButtonId, true)]
    public async Task OpenFromButtonAsync()
        => await OpenAsync(ephemeral: true);

    [ComponentInteraction(ChannelBrowserComponentIds.BackButtonId, true)]
    public async Task BackAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.Back();
        state.Notice = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(ChannelBrowserComponentIds.FilterSelectId, true)]
    public async Task SelectFilterAsync(string filter)
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        if (!Enum.TryParse(filter, ignoreCase: true, out ChannelBrowserFilter parsed))
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.Filter = parsed;
        state.Notice = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(ChannelBrowserComponentIds.ChannelSelectId, true)]
    public async Task SelectChannelAsync(IGuildChannel[] channels)
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        var channel = channels.FirstOrDefault();
        if (channel is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.Select(channel.Id);
        state.Notice = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(ChannelBrowserComponentIds.RefreshButtonId, true)]
    public async Task RefreshAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.Reload(Context.Guild);
        state.Notice = null;
        paginator.PageCount = state.GetPageCount();
        if (paginator.CurrentPageIndex >= paginator.PageCount)
            paginator.SetPage(Math.Max(0, paginator.PageCount - 1));

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(ChannelBrowserComponentIds.ClearButtonId, true)]
    public async Task ClearSearchAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.ClearSearch();
        state.Notice = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(ChannelBrowserComponentIds.SearchButtonId, true)]
    public async Task SearchAsync()
    {
        if (!TryGetPanel(out _, out _))
            return;

        await RespondWithModalAsync<ChannelSearchModal>(ChannelBrowserComponentIds.SearchModalId);
    }

    [ModalInteraction(ChannelBrowserComponentIds.SearchModalId, true)]
    public async Task SearchModalAsync(ChannelSearchModal modal)
    {
        if (!TryGetPanelFromModal(out var paginator, out var state))
        {
            await RespondAsync("This panel is no longer active.", ephemeral: true);
            return;
        }

        await DeferAsync();

        state.ApplySearch(modal.Query);
        state.Notice = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await RenderAsync(paginator);
    }

    private async Task OpenAsync(bool ephemeral)
    {
        await DeferAsync(ephemeral);

        var state = ChannelBrowserState.Create(Context.Guild);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithUserState(state)
            .WithPageCount(state.GetPageCount())
            .WithPageFactory(ChannelBrowserRenderer.GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: ephemeral,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    private bool TryGetPanel(out IComponentPaginator paginator, out ChannelBrowserState state)
    {
        if (Context.Interaction is not IComponentInteraction i
            || !interactive.TryGetComponentPaginator(i.Message, out var p)
            || p is null
            || !p.CanInteract(i.User))
        {
            paginator = null!;
            state = null!;
            return false;
        }

        paginator = p;
        state = paginator.GetUserState<ChannelBrowserState>();
        return true;
    }

    private bool TryGetPanelFromModal(out IComponentPaginator paginator, out ChannelBrowserState state)
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
        state = paginator.GetUserState<ChannelBrowserState>();
        return true;
    }

    private async Task RenderAsync(IComponentPaginator paginator)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(Context.Interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    public sealed class ChannelSearchModal : IModal
    {
        public string Title => "Channel Search";

        [InputLabel("Search by name or ID")]
        [ModalTextInput(ChannelBrowserComponentIds.SearchInputId, TextInputStyle.Short, placeholder: "e.g. general, 123456789", maxLength: 100)]
        public string? Query { get; set; }
    }
}

