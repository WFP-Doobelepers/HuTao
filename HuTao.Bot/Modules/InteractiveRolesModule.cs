using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Roles;

namespace HuTao.Bot.Modules;

[RequireContext(ContextType.Guild)]
public sealed class InteractiveRolesModule(InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("roles", "Browse server roles interactively.")]
    public async Task RolesAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);

        var state = RoleBrowserState.Create(Context.Guild);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithUserState(state)
            .WithPageCount(state.GetPageCount())
            .WithPageFactory(RoleBrowserRenderer.GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: ephemeral,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    [ComponentInteraction(RoleBrowserComponentIds.OpenButtonId, true)]
    public async Task OpenFromButtonAsync()
    {
        await DeferAsync(ephemeral: true);

        var state = RoleBrowserState.Create(Context.Guild);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithUserState(state)
            .WithPageCount(state.GetPageCount())
            .WithPageFactory(RoleBrowserRenderer.GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: true,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    [ComponentInteraction(RoleBrowserComponentIds.RoleSelectId, true)]
    public async Task SelectRoleAsync(IRole[] roles)
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        var role = roles.Length > 0 ? roles[0] : null;
        if (role is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.SelectRole(role.Id);
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(RoleBrowserComponentIds.BackButtonId, true)]
    public async Task BackAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.BackToList();
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(RoleBrowserComponentIds.ClearButtonId, true)]
    public async Task ClearFilterAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.ClearFilter();
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(RoleBrowserComponentIds.RefreshButtonId, true)]
    public async Task RefreshAsync()
    {
        if (!TryGetPanel(out var paginator, out var state))
            return;

        state.Reload(Context.Guild);
        paginator.PageCount = state.GetPageCount();
        if (paginator.CurrentPageIndex >= paginator.PageCount)
            paginator.SetPage(Math.Max(0, paginator.PageCount - 1));

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(RoleBrowserComponentIds.SearchButtonId, true)]
    public async Task SearchAsync()
    {
        if (!TryGetPanel(out _, out _))
            return;

        await RespondWithModalAsync<RoleSearchModal>(RoleBrowserComponentIds.SearchModalId);
    }

    [ModalInteraction(RoleBrowserComponentIds.SearchModalId, true)]
    public async Task SearchModalAsync(RoleSearchModal modal)
    {
        if (!TryGetPanelFromModal(out var paginator, out var state))
        {
            await RespondAsync("This panel is no longer active.", ephemeral: true);
            return;
        }

        await DeferAsync();

        state.ApplyFilter(modal.Query);
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await RenderAsync(paginator);
    }

    private bool TryGetPanel(out IComponentPaginator paginator, out RoleBrowserState state)
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
        state = paginator.GetUserState<RoleBrowserState>();
        return true;
    }

    private bool TryGetPanelFromModal(out IComponentPaginator paginator, out RoleBrowserState state)
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
        state = paginator.GetUserState<RoleBrowserState>();
        return true;
    }

    private async Task RenderAsync(IComponentPaginator paginator)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(Context.Interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    public sealed class RoleSearchModal : IModal
    {
        public string Title => "Role Search";

        [InputLabel("Filter roles by name or ID")]
        [ModalTextInput(RoleBrowserComponentIds.SearchInputId, TextInputStyle.Short, placeholder: "e.g. moderator, 123456789", maxLength: 100)]
        public string? Query { get; set; }
    }
}

