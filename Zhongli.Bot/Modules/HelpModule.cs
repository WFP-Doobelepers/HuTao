using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Fergun.Interactive;
using Zhongli.Data.Config;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules;

[Name("Help")]
[Group("help")]
[Summary("Provides commands for helping users to understand how to interact with the bot.")]
public sealed class HelpModule : ModuleBase
{
    private readonly ICommandHelpService _commandHelpService;
    private readonly InteractiveService _interactive;

    public HelpModule(ICommandHelpService commandHelpService, InteractiveService interactive)
    {
        _commandHelpService = commandHelpService;
        _interactive        = interactive;
    }

    [Command]
    [Summary("Prints a neat list of all commands.")]
    public async Task HelpAsync()
    {
        var modules = _commandHelpService.GetModuleHelpData()
            .OrderBy(d => d.Name)
            .Select(d => Format.Bold(Format.Code(d.Name)));

        var prefix = ZhongliConfig.Configuration.Prefix;
        var descriptionBuilder = new StringBuilder()
            .AppendLine("Modules:")
            .AppendJoin(", ", modules)
            .AppendLine().AppendLine()
            .AppendLine($"Do {Format.Code($"{prefix}help dm")} to have everything DMed to you.")
            .AppendLine($"Do {Format.Code($"{prefix}help [module name]")} to have that module's commands listed.");

        var argumentBuilder = new StringBuilder()
            .AppendLine($"{Format.Code("[ ]")}: Optional arguments.")
            .AppendLine($"{Format.Code("< >")}: Required arguments.")
            .AppendLine($"{Format.Code("[...]")}: List of arguments separated by {Format.Code(",")}.")
            .AppendLine(
                $"▌Provide values by doing {Format.Code("name: value")} " +
                $"or {Format.Code("name: \"value with spaces\"")}.");

        var embed = new EmbedBuilder()
            .WithTitle("Help")
            .WithCurrentTimestamp()
            .WithDescription(descriptionBuilder.ToString())
            .AddField("Arguments", argumentBuilder.ToString())
            .WithGuildAsAuthor(Context.Guild, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await ReplyAsync(embed: embed.Build());
    }

    [Command]
    [Summary("Retrieves help from a specific module or command.")]
    [Priority(-10)]
    public async Task HelpAsync(
        [Remainder] [Summary("Name of the module or command to query.")]
        string query)
    {
        await HelpAsync(query, HelpDataType.Command | HelpDataType.Module);
    }

    [Command("command")]
    [Alias("commands")]
    [Summary("Retrieves help from a specific command. Useful for commands that have an overlapping module name.")]
    public async Task HelpCommandAsync(
        [Remainder] [Summary("Name of the module to query.")]
        string query)
    {
        await HelpAsync(query, HelpDataType.Command);
    }

    [Command("dm")]
    [Summary("Spams the user's DMs with a list of every command available.")]
    public async Task HelpDMAsync()
    {
        var tokenSource = new CancellationTokenSource();
        var dm = await Context.User.CreateDMChannelAsync();
        var modules = _commandHelpService
            .GetModuleHelpData()
            .OrderBy(x => x.Name);

        foreach (var module in modules)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var paginator = _commandHelpService.GetEmbedForModule(module);
                    await _interactive.SendPaginatorAsync(paginator.WithUsers(Context.User).Build(), dm, resetTimeoutOnInput: true, cancellationToken: tokenSource.Token);
                }
                catch (HttpException ex) when (ex.DiscordCode is DiscordErrorCode.CannotSendMessageToUser)
                {
                    tokenSource.Cancel();
                }
            }, tokenSource.Token);
        }

        if (tokenSource.IsCancellationRequested)
            await ReplyAsync($"You have private messages for this server disabled, {Context.User.Mention}. Please enable them so that I can send you help.");
        else
            await ReplyAsync($"Check your private messages, {Context.User.Mention}.");
    }

    [Command("module")]
    [Alias("modules")]
    [Summary("Retrieves help from a specific module. Useful for modules that have an overlapping command name.")]
    public async Task HelpModuleAsync(
        [Remainder] [Summary("Name of the module to query.")]
        string query)
    {
        await HelpAsync(query, HelpDataType.Module);
    }

    private async Task HelpAsync(string query, HelpDataType type)
    {
        var sanitizedQuery = FormatUtilities.SanitizeAllMentions(query);

        if (_commandHelpService.TryGetEmbed(query, type, out var paginated))
            await _interactive.SendPaginatorAsync(paginated.WithUsers(Context.User).Build(), Context.Channel, resetTimeoutOnInput: true);
        else
            await ReplyAsync($"Sorry, I couldn't find help related to \"{sanitizedQuery}\".");
    }
}