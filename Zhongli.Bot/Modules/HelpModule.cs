using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
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

            var descriptionBuilder = new StringBuilder()
                .AppendLine("Modules:")
                .AppendJoin(", ", modules)
                .AppendLine().AppendLine()
                .AppendLine("Do \"z!help dm\" to have everything DMed to you. (Spammy!)")
                .AppendLine("Do \"z!help [module name] to have that module's commands listed.");

            var embed = new EmbedBuilder()
                .WithTitle("Help")
                .WithDescription(descriptionBuilder.ToString());

            await ReplyAsync(embed: embed.Build());
        }

        [Command("dm")]
        [Summary("Spams the user's DMs with a list of every command available.")]
        public async Task HelpDMAsync()
        {
            var userDM = await Context.User.GetOrCreateDMChannelAsync();

            foreach (var module in _commandHelpService.GetModuleHelpData().OrderBy(x => x.Name))
            {
                var embed = GetEmbedForModule(module);

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

        [Command]
        [Summary("Retrieves help from a specific module or command.")]
        [Priority(-10)]
        public async Task HelpAsync(
            [Remainder] [Summary("Name of the module or command to query.")]
            string query)
        {
            await HelpAsync(query, HelpDataType.Command | HelpDataType.Module);
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

        [Command("command")]
        [Alias("commands")]
        [Summary("Retrieves help from a specific command. Useful for commands that have an overlapping module name.")]
        public async Task HelpCommandAsync(
            [Remainder] [Summary("Name of the module to query.")]
            string query)
        {
            await HelpAsync(query, HelpDataType.Command);
        }

        private async Task HelpAsync(string query, HelpDataType type)
        {
            var sanitizedQuery = FormatUtilities.SanitizeAllMentions(query);

            if (TryGetEmbed(query, type, out var embed))
            {
                await ReplyAsync($"Results for \"{sanitizedQuery}\":", embed: embed.Build());
                return;
            }

            await ReplyAsync($"Sorry, I couldn't find help related to \"{sanitizedQuery}\".");
        }

        private bool TryGetEmbed(string query, HelpDataType queries, out EmbedBuilder embed)
        {
            embed = null;

            // Prioritize module over command.
            if (queries.HasFlag(HelpDataType.Module))
            {
                var byModule = _commandHelpService.GetModuleHelpData(query);
                if (byModule != null)
                {
                    embed = GetEmbedForModule(byModule);
                    return true;
                }
            }

            if (queries.HasFlag(HelpDataType.Command))
            {
                var byCommand = _commandHelpService.GetCommandHelpData(query);
                if (byCommand != null)
                {
                    embed = GetEmbedForCommand(byCommand);
                    return true;
                }
            }

            return false;
        }

        private EmbedBuilder GetEmbedForModule(ModuleHelpData module)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle($"Module: {module.Name}")
                .WithDescription(module.Summary);

            foreach (var command in module.Commands)
            {
                AddCommandFields(embedBuilder, command);
            }

            return embedBuilder;
        }

        private EmbedBuilder GetEmbedForCommand(CommandHelpData command) =>
            AddCommandFields(new EmbedBuilder(), command);

        private EmbedBuilder AddCommandFields(EmbedBuilder embedBuilder, CommandHelpData command)
        {
            var summaryBuilder = new StringBuilder(command.Summary ?? "No summary.").AppendLine();
            var name = command.Aliases.FirstOrDefault();
            AppendAliases(summaryBuilder,
                command.Aliases.Where(a => !a.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList());
            AppendParameters(summaryBuilder, command.Parameters);

            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithName($"Command: z!{name} {GetParams(command)}")
                .WithValue(summaryBuilder.ToString()));

            return embedBuilder;
        }

        private StringBuilder AppendAliases(StringBuilder stringBuilder, IReadOnlyCollection<string> aliases)
        {
            if (aliases.Count == 0)
                return stringBuilder;

            stringBuilder.AppendLine(Format.Bold("Aliases:"));

            foreach (var alias in FormatUtilities.CollapsePlurals(aliases))
            {
                stringBuilder.AppendLine($"• {alias}");
            }

            return stringBuilder;
        }

        private StringBuilder AppendParameters(StringBuilder stringBuilder,
            IReadOnlyCollection<ParameterHelpData> parameters)
        {
            var includedParameters = parameters
                .Where(p => p.Summary is not null)
                .ToList();

            if (includedParameters.Count == 0)
                return stringBuilder;

            stringBuilder.AppendLine(Format.Bold("Parameters:"));

            foreach (var parameter in includedParameters)
            {
                stringBuilder.AppendLine($"• {Format.Bold(parameter.Name)}: {parameter.Summary}");
            }

            return stringBuilder;
        }

        private static string GetParams(CommandHelpData info)
        {
            var parameters = info.Parameters
                .Select(p => p.IsOptional ? $"[{p.Name}]" : $"<{p.Name}>");

            return string.Join(" ", parameters);
        }

        [Flags]
        private enum HelpDataType
        {
            Command = 1 << 1,
            Module = 1 << 2
        }
    }
}