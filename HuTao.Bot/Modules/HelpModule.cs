using System.Linq;
using System.Text;
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
        try
        {
            var dm = await Context.User.CreateDMChannelAsync();
            var state = HelpBrowserState.Create(commandHelpService.GetModuleHelpData(), HuTaoConfig.Configuration.Prefix);

            var browser = InteractiveExtensions.CreateDefaultComponentPaginator()
                .WithUsers(Context.User)
                .WithPageCount(state.GetPageCount())
                .WithUserState(state)
                .WithPageFactory(HelpBrowserRenderer.GeneratePage)
                .Build();

            await interactive.SendPaginatorAsync(browser, dm, resetTimeoutOnInput: true);

            var ok = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay($"## Help\nSent you an interactive help browser in DMs, {Context.User.Mention}.")
                    .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay("-# Tip: use `/help` for an ephemeral version.")
                    .WithAccentColor(0x9B59FF))
                .Build();

            await ReplyAsync(components: ok, allowedMentions: AllowedMentions.None);
        }
        catch (HttpException ex) when (ex.DiscordCode is DiscordErrorCode.CannotSendMessageToUser)
        {
            var blocked = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay($"## Help\nI can't DM you, {Context.User.Mention}.")
                    .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay("-# Enable server DMs, then try again.")
                    .WithAccentColor(0x9B59FF))
                .Build();

            await ReplyAsync(components: blocked, allowedMentions: AllowedMentions.None);
        }
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

        var state = HelpBrowserState.Create(commandHelpService.GetModuleHelpData(), HuTaoConfig.Configuration.Prefix);
        state.TryApplyQuery(sanitizedQuery, type);

        var browser = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(state.GetPageCount())
            .WithUserState(state)
            .WithPageFactory(HelpBrowserRenderer.GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(browser, Context.Channel, resetTimeoutOnInput: true);
    }
}