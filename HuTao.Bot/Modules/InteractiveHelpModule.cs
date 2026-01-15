using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data.Config;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules;

[RequireContext(ContextType.Guild)]
public sealed class InteractiveHelpModule(ICommandHelpService help, InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    [SlashCommand("help", "Browse bot commands and usage.")]
    public async Task HelpAsync(
        [Summary("Search by module/tag/command name (optional).")]
        [Autocomplete(typeof(HelpAutocomplete))]
        string? query = null,
        [Summary("True to only show you the help.")]
        [RequireEphemeralScope]
        bool ephemeral = false)
    {
        await DeferAsync(ephemeral);

        var state = HelpBrowserState.Create(help.GetModuleHelpData(), HuTaoConfig.Configuration.Prefix);

        if (!string.IsNullOrWhiteSpace(query))
        {
            state.TryApplyQuery(FormatUtilities.SanitizeAllMentions(query));
        }

        var browser = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(state.GetPageCount())
            .WithUserState(state)
            .WithPageFactory(HelpBrowserRenderer.GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            browser,
            Context.Interaction,
            ephemeral: ephemeral,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    [ComponentInteraction(HelpBrowserComponentIds.ModuleSelectId, true)]
    public async Task SelectModuleAsync(string moduleIndex)
    {
        if (!TryGetBrowser(out var paginator, out var state, out _))
            return;

        if (!int.TryParse(moduleIndex, out var index) || index < 0 || index >= state.Modules.Count)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.View = HelpBrowserView.ModuleCommands;
        state.SelectedModuleIndex = index;
        state.SelectedCommandIndex = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(HelpBrowserComponentIds.CommandSelectId, true)]
    public async Task SelectCommandAsync(string commandIndex)
    {
        if (!TryGetBrowser(out var paginator, out var state, out _))
            return;

        var commands = state.GetSelectedCommands();
        if (commands is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (!int.TryParse(commandIndex, out var index) || index < 0 || index >= commands.Count)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.View = HelpBrowserView.CommandDetail;
        state.SelectedCommandIndex = index;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(HelpBrowserComponentIds.BackButtonId, true)]
    public async Task BackAsync()
    {
        if (!TryGetBrowser(out var paginator, out var state, out _))
            return;

        state.View = state.View switch
        {
            HelpBrowserView.CommandDetail => HelpBrowserView.ModuleCommands,
            HelpBrowserView.ModuleCommands => HelpBrowserView.Modules,
            _ => HelpBrowserView.Modules
        };

        state.SelectedCommandIndex = null;
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(HelpBrowserComponentIds.TagSelectId, true)]
    public async Task SelectTagAsync(string tagValue)
    {
        if (!TryGetBrowser(out var paginator, out var state, out _))
            return;

        state.TagFilter = tagValue == "__all__" ? null : tagValue;
        state.Notice = null;
        state.View = HelpBrowserView.Modules;
        state.SelectedModuleIndex = null;
        state.SelectedCommandIndex = null;

        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(HelpBrowserComponentIds.SearchButtonId, true)]
    public async Task SearchAsync()
    {
        if (!TryGetBrowser(out _, out _, out var interaction))
            return;

        await RespondWithModalAsync<HelpSearchModal>(HelpBrowserComponentIds.SearchModalId);
    }

    [ModalInteraction(HelpBrowserComponentIds.SearchModalId, true)]
    public async Task SearchModalAsync(HelpSearchModal modal)
    {
        if (!TryGetBrowserFromModal(out var paginator, out var state))
        {
            await RespondAsync("This panel is no longer active.", ephemeral: true);
            return;
        }

        await DeferAsync();

        state.TryApplyQuery(modal.Query);
        paginator.PageCount = state.GetPageCount();
        paginator.SetPage(0);

        await RenderAsync(paginator);
    }

    private bool TryGetBrowser(out IComponentPaginator paginator, out HelpBrowserState state, out IComponentInteraction interaction)
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
        state = paginator.GetUserState<HelpBrowserState>();
        return true;
    }

    private bool TryGetBrowserFromModal(out IComponentPaginator paginator, out HelpBrowserState state)
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
        state = paginator.GetUserState<HelpBrowserState>();
        return true;
    }

    private async Task RenderAsync(IComponentPaginator paginator)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(Context.Interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    public sealed class HelpSearchModal : IModal
    {
        public string Title => "Help Search";

        [InputLabel("Search (module / tag / command)")]
        [ModalTextInput(HelpBrowserComponentIds.SearchInputId, TextInputStyle.Short, placeholder: "e.g. moderation, log, ban", maxLength: 100)]
        public string? Query { get; set; }
    }
}

