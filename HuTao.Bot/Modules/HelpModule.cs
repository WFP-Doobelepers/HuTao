using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Data.Config;
using HuTao.Services.CommandHelp;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules;

[Name("Help")]
[Group("help")]
[Summary("Provides commands for helping users to understand how to interact with the bot.")]
public sealed class HelpModule(ICommandHelpService commandHelpService, InteractiveService interactive)
    : ModuleBase
{
    [Command]
    [Summary("Prints a neat list of all commands.")]
    public async Task HelpAsync()
    {
        var state = HelpBrowserState.Create(commandHelpService.GetModuleHelpData(), HuTaoConfig.Configuration.Prefix);

        var browser = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(state.GetPageCount())
            .WithUserState(state)
            .WithPageFactory(HelpBrowserRenderer.GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(browser, Context.Channel, resetTimeoutOnInput: true);
    }

    [Command]
    [Summary("Retrieves help from a specific module or command.")]
    [Priority(-10)]
    public async Task HelpAsync(
        [Remainder] [Summary("Name of the module or command to query.")]
        string query)
        => await HelpAsync(query, HelpDataType.Command | HelpDataType.Module);

    [Command("command")]
    [Alias("commands")]
    [Summary("Retrieves help from a specific command. Useful for commands that have an overlapping module name.")]
    public async Task HelpCommandAsync(
        [Remainder] [Summary("Name of the module to query.")]
        string query)
        => await HelpAsync(query, HelpDataType.Command);

    [Command("dm")]
    [Summary("Spams the user's DMs with a list of every command available.")]
    public async Task HelpDMAsync()
    {
        var tokenSource = new CancellationTokenSource();
        var dm = await Context.User.CreateDMChannelAsync();
        var modules = commandHelpService
            .GetModuleHelpData()
            .OrderBy(x => x.Name);

        foreach (var module in modules)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var paginator = commandHelpService.GetPaginatorForModule(module);
                    await interactive.SendPaginatorAsync(paginator.WithUsers(Context.User).Build(), dm,
                        resetTimeoutOnInput: true, cancellationToken: tokenSource.Token);
                }
                catch (HttpException ex) when (ex.DiscordCode is DiscordErrorCode.CannotSendMessageToUser)
                {
                    tokenSource.Cancel();
                }
            }, tokenSource.Token);
        }

        if (tokenSource.IsCancellationRequested)
        {
            await ReplyAsync(
                $"You have private messages for this server disabled, {Context.User.Mention}. Please enable them so that I can send you help.");
        }
        else
            await ReplyAsync($"Check your private messages, {Context.User.Mention}.");
    }

    [Command("module")]
    [Alias("modules")]
    [Summary("Retrieves help from a specific module. Useful for modules that have an overlapping command name.")]
    public async Task HelpModuleAsync(
        [Remainder] [Summary("Name of the module to query.")]
        string query)
        => await HelpAsync(query, HelpDataType.Module);

    private async Task HelpAsync(string query, HelpDataType type)
    {
        var sanitizedQuery = FormatUtilities.SanitizeAllMentions(query);

        if (commandHelpService.TryGetPaginator(query, type, out var paginated))
        {
            await interactive.SendPaginatorAsync(paginated.WithUsers(Context.User).Build(), Context.Channel,
                resetTimeoutOnInput: true);
        }
        else
            await ReplyAsync($"Sorry, I couldn't find help related to \"{sanitizedQuery}\".");
    }
}