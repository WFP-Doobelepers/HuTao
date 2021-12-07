using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Discord;
using Discord.Addons.Interactive.Paginator;
using Discord.Commands;

namespace Zhongli.Services.CommandHelp
{
    /// <summary>
    ///     Provides functionality to retrieve command help information.
    /// </summary>
    public interface ICommandHelpService
    {
        bool TryGetEmbed(string query, HelpDataType queries, out PaginatedMessage embed);

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
        PaginatedMessage GetEmbedForCommand(CommandHelpData command);

        /// <summary>
        ///     Retrieves an embed from a <see cref="ModuleHelpData" />
        /// </summary>
        /// <param name="module">The module's help data.</param>
        /// <returns>An <see cref="EmbedBuilder" /> that contains information for the module.</returns>
        PaginatedMessage GetEmbedForModule(ModuleHelpData module);
    }

    /// <inheritdoc />
    internal class CommandHelpService : ICommandHelpService
    {
        private readonly CommandService _commandService;

        private readonly PaginatedAppearanceOptions _paginatedOptions = new()
        {
            DisplayInformationIcon = false,
            Timeout                = TimeSpan.FromMinutes(10)
        };

        private IReadOnlyCollection<ModuleHelpData> _cachedHelpData = null!;

        public CommandHelpService(CommandService commandService) { _commandService = commandService; }

        public bool TryGetEmbed(string query, HelpDataType queries, out PaginatedMessage message)
        {
            message = null!;

            // Prioritize module over command.
            if (queries.HasFlag(HelpDataType.Module))
            {
                var byModule = GetModuleHelpData(query);
                if (byModule is not null)
                {
                    message = GetEmbedForModule(byModule);
                    return true;
                }
            }

            if (queries.HasFlag(HelpDataType.Command))
            {
                var byCommand = GetCommandHelpData(query);
                if (byCommand is not null)
                {
                    message = GetEmbedForCommand(byCommand);
                    return true;
                }
            }

            return false;
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

        public PaginatedMessage GetEmbedForCommand(CommandHelpData command)
        {
            var embed = new EmbedBuilder()
                .AddCommandFields(command);

            return new PaginatedMessage
            {
                Pages   = embed.Fields,
                Options = _paginatedOptions
            };
        }

        public PaginatedMessage GetEmbedForModule(ModuleHelpData module)
        {
            var embed = new EmbedBuilder();
            foreach (var command in module.Commands)
            {
                embed.AddCommandFields(command);
            }

            return new PaginatedMessage
            {
                Title                = $"Module: {module.Name}",
                AlternateDescription = module.Summary,
                Pages                = embed.Fields,
                Options              = _paginatedOptions
            };
        }
    }

    [Flags]
    public enum HelpDataType
    {
        Command = 1 << 1,
        Module = 1 << 2
    }
}