using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Microsoft.Extensions.Configuration;
using Zhongli.Data.Config;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules
{
    [Name("Help")]
    [Group("help")]
    [Summary("Provides commands for helping users to understand how to interact with the bot.")]
    public sealed class HelpModule : ModuleBase
    {
        private readonly ICommandHelpService _commandHelpService;

        public HelpModule(ICommandHelpService commandHelpService) { _commandHelpService = commandHelpService; }

        [Command]
        [Summary("Prints a neat list of all commands.")]
        public async Task HelpAsync()
        {
            var modules = _commandHelpService.GetModuleHelpData()
                .Select(d => d.Name)
                .OrderBy(d => d);

            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<ZhongliConfig>()
                .Build();

            var prefix = string.Empty;

#if DEBUG
            prefix = configuration.GetSection(nameof(ZhongliConfig.Debug))[nameof(BotConfig.Prefix)];
#else
            prefix = configuration.GetSection(nameof(ZhongliConfig.Release))[nameof(BotConfig.Prefix)];
#endif

            var descriptionBuilder = new StringBuilder()
                .AppendLine("Modules:")
                .AppendJoin(", ", modules)
                .AppendLine().AppendLine()
                .AppendLine($"Do \"{prefix}help dm\" to have everything DMed to you. (Spammy!)")
                .AppendLine($"Do \"{prefix}help [module name] to have that module's commands listed.");

            var embed = new EmbedBuilder()
                .WithTitle("Help")
                .WithDescription(descriptionBuilder.ToString());

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
            var userDM = await Context.User.GetOrCreateDMChannelAsync();

            foreach (var module in _commandHelpService.GetModuleHelpData().OrderBy(x => x.Name))
            {
                var embed = _commandHelpService.GetEmbedForModule(module);

                try
                {
                    await userDM.SendMessageAsync(embed: embed.Build());
                }
                catch (HttpException ex) when (ex.DiscordCode == 50007)
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

            if (_commandHelpService.TryGetEmbed(query, type, out var embed))
            {
                await ReplyAsync($"Results for \"{sanitizedQuery}\":", embed: embed.Build());
                return;
            }

            await ReplyAsync($"Sorry, I couldn't find help related to \"{sanitizedQuery}\".");
        }
    }
}