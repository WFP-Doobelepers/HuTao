using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;

namespace HuTao.Services.CommandHelp;

/// <summary>
///     Provides functionality to retrieve command help information.
/// </summary>
public interface ICommandHelpService
{
    bool TryGetPaginator(string query, HelpDataType queries, out ComponentPaginatorBuilder paginator);

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
    /// <returns>A component paginator builder that contains information for the command.</returns>
    ComponentPaginatorBuilder GetPaginatorForCommand(CommandHelpData command);

    /// <summary>
    ///     Retrieves an embed from a <see cref="ModuleHelpData" />
    /// </summary>
    /// <param name="module">The module's help data.</param>
    /// <returns>A component paginator builder that contains information for the module.</returns>
    ComponentPaginatorBuilder GetPaginatorForModule(ModuleHelpData module);

    /// <summary>
    ///     Builds a single Components V2 help message for a module (static, no paginator controls).
    /// </summary>
    MessageComponent GetComponentsForModule(ModuleHelpData module, int pageSize = 8);
}

/// <inheritdoc />
internal class CommandHelpService(CommandService commandService) : ICommandHelpService
{
    private IReadOnlyCollection<ModuleHelpData> _cachedHelpData = null!;

    public bool TryGetPaginator(string query, HelpDataType queries, out ComponentPaginatorBuilder message)
    {
        message = null!;

        // Prioritize module over command.
        if (queries.HasFlag(HelpDataType.Module))
        {
            var byModule = GetModuleHelpData(query);
            if (byModule is not null)
            {
                message = GetPaginatorForModule(byModule);
                return true;
            }
        }

        if (queries.HasFlag(HelpDataType.Command))
        {
            var byCommand = GetCommandHelpData(query);
            if (byCommand is not null)
            {
                message = GetPaginatorForCommand(byCommand);
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
            commandService.Modules
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

    public ComponentPaginatorBuilder GetPaginatorForCommand(CommandHelpData command)
    {
        const uint accentColor = 0x9B59FF;
        var embed = command.ToEmbedBuilder().Build();
        var text = embed.ToComponentsV2Text(maxChars: 3800);

        return InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithPageCount(1)
            .WithPageFactory(p =>
            {
                var container = new ContainerBuilder()
                    .WithTextDisplay(text)
                    .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small)
                    .WithActionRow(new ActionRowBuilder()
                        .AddStopButton(p, "Close", ButtonStyle.Danger))
                    .WithAccentColor(accentColor);

                var components = new ComponentBuilderV2().WithContainer(container).Build();
                return new PageBuilder().WithComponents(components).Build();
            });
    }

    public ComponentPaginatorBuilder GetPaginatorForModule(ModuleHelpData module)
    {
        const int pageSize = 6;
        var prefix = HuTao.Data.Config.HuTaoConfig.Configuration.Prefix;
        var commands = module.Commands
            .OrderBy(c => c.Aliases.FirstOrDefault() ?? c.Name)
            .ToList();

        var pageCount = Math.Max(1, (int)Math.Ceiling((double)commands.Count / pageSize));

        return InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithPageCount(pageCount)
            .WithPageFactory(p => BuildModuleHelpPage(p, module, commands, pageSize, prefix));
    }

    public MessageComponent GetComponentsForModule(ModuleHelpData module, int pageSize = 8)
    {
        const uint accentColor = 0x9B59FF;
        var prefix = HuTao.Data.Config.HuTaoConfig.Configuration.Prefix;
        var commands = module.Commands
            .OrderBy(c => c.Aliases.FirstOrDefault() ?? c.Name)
            .Take(Math.Max(1, pageSize))
            .ToList();

        var container = BuildModuleContainer(module, commands, prefix)
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay($"-# Tip: use {Format.Code($"{prefix}help command <name>")} for full command details")
            .WithAccentColor(accentColor);

        return new ComponentBuilderV2().WithContainer(container).Build();
    }

    private static IPage BuildModuleHelpPage(
        IComponentPaginator p,
        ModuleHelpData module,
        IReadOnlyList<CommandHelpData> commands,
        int pageSize,
        string prefix)
    {
        const uint accentColor = 0x9B59FF;

        var pageCommands = commands
            .Skip(p.CurrentPageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var container = BuildModuleContainer(module, pageCommands, prefix)
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small)
            .WithActionRow(new ActionRowBuilder()
                .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
                .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
                .AddNextButton(p, "▶", ButtonStyle.Secondary)
                .AddStopButton(p, "Close", ButtonStyle.Danger))
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay($"-# Page {p.CurrentPageIndex + 1} of {p.PageCount} • Use {Format.Code($"{prefix}help command <name>")} for full details")
            .WithAccentColor(accentColor);

        var components = new ComponentBuilderV2().WithContainer(container).Build();
        return new PageBuilder().WithComponents(components).Build();
    }

    private static ContainerBuilder BuildModuleContainer(
        ModuleHelpData module,
        IReadOnlyList<CommandHelpData> commands,
        string prefix)
    {
        const int maxSummaryChars = 220;
        var container = new ContainerBuilder()
            .WithTextDisplay($"## Module: {module.Name}\n{module.Summary ?? "No summary."}")
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        for (var i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            var name = command.Aliases.FirstOrDefault() ?? command.Name;
            var summary = (command.Summary ?? "No summary.").Truncate(maxSummaryChars);

            var aliases = command.Aliases
                .Where(a => !a.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            var text = new StringBuilder()
                .AppendLine($"### {prefix}{name}")
                .AppendLine(summary)
                .AppendLine(aliases.Count != 0
                    ? $"-# Aliases: {string.Join(", ", aliases.Select(a => Format.Code(a)))}"
                    : string.Empty)
                .ToString()
                .Trim();

            container.WithTextDisplay(text);

            if (i < commands.Count - 1)
                container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        return container;
    }
}

[Flags]
public enum HelpDataType
{
    Command = 1 << 1,
    Module = 1 << 2
}