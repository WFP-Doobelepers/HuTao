using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Interactivity;
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
    private readonly InteractivityService _interactivity;

    public HelpModule(ICommandHelpService commandHelpService, InteractivityService interactivity)
    {
        _commandHelpService = commandHelpService;
        _interactivity      = interactivity;
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
        var userDM = await Context.User.CreateDMChannelAsync();

        foreach (var module in _commandHelpService.GetModuleHelpData().OrderBy(x => x.Name))
        {
            var paginator = _commandHelpService.GetEmbedForModule(module);

            try
            {
                var dm = await Context.User.CreateDMChannelAsync();
                await _interactivity.SendPaginatorAsync(paginator.Build(), dm);
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                await ReplyAsync(
                    $"You have private messages for this server disabled, {Context.User.Mention}. Please enable them so that I can send you help.");
                return;
            }
        }

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
            await _interactivity.SendPaginatorAsync(paginated, Context.Channel);
        else
            await ReplyAsync($"Sorry, I couldn't find help related to \"{sanitizedQuery}\".");
    }
}