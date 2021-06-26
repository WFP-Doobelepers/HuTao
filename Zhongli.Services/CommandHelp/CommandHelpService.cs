using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Discord;
using Discord.Commands;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.CommandHelp
{
    /// <summary>
    ///     Provides functionality to retrieve command help information.
    /// </summary>
    public interface ICommandHelpService
    {
        /// <summary>
        ///     Retrieves command help data for the supplied query.
        /// </summary>
        /// <param name="query">A query to use to search for an applicable help module.</param>
        /// <returns>
        ///     Help information for the supplied query, or <see langword="null" /> if no information could be found for the
        ///     supplied query.
        /// </returns>
        CommandHelpData? GetCommandHelpData(string query);

        /// <summary>
        ///     Retrieves help data for all available modules.
        /// </summary>
        /// <returns>
        ///     A readonly collection of data about all available modules.
        /// </returns>
        IReadOnlyCollection<ModuleHelpData> GetModuleHelpData();

        /// <summary>
        ///     Retrieves module help data for the supplied query.
        /// </summary>
        /// <param name="query">A query to use to search for an applicable help module.</param>
        /// <returns>
        ///     Help information for the supplied query, or <see langword="null" /> if no information could be found for the
        ///     supplied query.
        /// </returns>
        ModuleHelpData? GetModuleHelpData(string query);

        /// <summary>
        ///     Retrieves an embed from a <see cref="CommandHelpData" />.
        /// </summary>
        /// <param name="command">The command's help data.</param>
        /// <returns>An <see cref="EmbedBuilder" /> that contains information for the command.</returns>
        EmbedBuilder GetEmbedForCommand(CommandHelpData command);

        /// <summary>
        ///     Retrieves an embed from a <see cref="ModuleHelpData" />
        /// </summary>
        /// <param name="module">The module's help data.</param>
        /// <returns>An <see cref="EmbedBuilder" /> that contains information for the module.</returns>
        EmbedBuilder GetEmbedForModule(ModuleHelpData module);

        bool TryGetEmbed(string query, HelpDataType queries, out EmbedBuilder embed);
    }

    /// <inheritdoc />
    internal class CommandHelpService : ICommandHelpService
    {
        private readonly CommandService _commandService;
        private IReadOnlyCollection<ModuleHelpData> _cachedHelpData = null!;

        public CommandHelpService(CommandService commandService) { _commandService = commandService; }

        /// <inheritdoc />
        public IReadOnlyCollection<ModuleHelpData> GetModuleHelpData()
            => LazyInitializer.EnsureInitialized(ref _cachedHelpData, () =>
                _commandService.Modules
                    .Where(x => !x.Attributes.Any(attr => attr is HiddenFromHelpAttribute))
                    .Select(ModuleHelpData.FromModuleInfo)
                    .ToArray());

        /// <inheritdoc />
        public ModuleHelpData? GetModuleHelpData(string query)
        {
            var allHelpData = GetModuleHelpData();

            var byNameExact = allHelpData.FirstOrDefault(x => x.Name.Equals(query, StringComparison.OrdinalIgnoreCase));
            if (byNameExact is not null)
                return byNameExact;

            var byTagsExact = allHelpData.FirstOrDefault(x =>
                x.HelpTags.Any(y => y.Equals(query, StringComparison.OrdinalIgnoreCase)));
            if (byTagsExact is not null)
                return byTagsExact;

            var byNameContains =
                allHelpData.FirstOrDefault(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            if (byNameContains is not null)
                return byNameContains;

            var byTagsContains = allHelpData.FirstOrDefault(x =>
                x.HelpTags.Any(y => y.Contains(query, StringComparison.OrdinalIgnoreCase)));
            if (byTagsContains is not null)
                return byTagsContains;

            return null;
        }

        /// <inheritdoc />
        public CommandHelpData? GetCommandHelpData(string query)
        {
            var allHelpData = GetModuleHelpData().SelectMany(x => x.Commands).ToList();

            var byModuleNameExact = allHelpData.FirstOrDefault(x =>
                x.Aliases.Any(y => y.Equals(query, StringComparison.OrdinalIgnoreCase)));
            if (byModuleNameExact is not null)
                return byModuleNameExact;

            var byNameContains =
                allHelpData.FirstOrDefault(x =>
                    x.Aliases.Any(y => y.Contains(query, StringComparison.OrdinalIgnoreCase)));
            if (byNameContains is not null)
                return byNameContains;

            return null;
        }

        public EmbedBuilder GetEmbedForCommand(CommandHelpData command) =>
            AddCommandFields(new EmbedBuilder(), command);

        public EmbedBuilder GetEmbedForModule(ModuleHelpData module)
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

        public bool TryGetEmbed(string query, HelpDataType queries, out EmbedBuilder embed)
        {
            embed = null!;

            // Prioritize module over command.
            if (queries.HasFlag(HelpDataType.Module))
            {
                var byModule = GetModuleHelpData(query);
                if (byModule is not null)
                {
                    embed = GetEmbedForModule(byModule);
                    return true;
                }
            }

            if (queries.HasFlag(HelpDataType.Command))
            {
                var byCommand = GetCommandHelpData(query);
                if (byCommand is not null)
                {
                    embed = GetEmbedForCommand(byCommand);
                    return true;
                }
            }

            return false;
        }

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

        private static string GetParams(CommandHelpData info)
        {
            var parameters = info.Parameters
                .Select(p => p.IsOptional ? $"[{p.Name}]" : $"<{p.Name}>");

            return string.Join(" ", parameters);
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
    }

    [Flags]
    public enum HelpDataType
    {
        Command = 1 << 1,
        Module = 1 << 2
    }
}