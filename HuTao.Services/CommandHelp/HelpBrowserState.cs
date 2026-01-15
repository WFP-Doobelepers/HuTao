using System;
using System.Collections.Generic;
using System.Linq;

namespace HuTao.Services.CommandHelp;

public sealed class HelpBrowserState
{
    private const int PageSize = 10;

    public required string Prefix { get; init; }
    public required IReadOnlyList<ModuleHelpData> Modules { get; init; }

    public HelpBrowserView View { get; set; } = HelpBrowserView.Modules;

    public int? SelectedModuleIndex { get; set; }
    public int? SelectedCommandIndex { get; set; }

    public string? TagFilter { get; set; }
    public string? Notice { get; set; }

    public static HelpBrowserState Create(IReadOnlyCollection<ModuleHelpData> modules, string prefix)
    {
        var ordered = modules.OrderBy(m => m.Name).ToList();
        return new HelpBrowserState
        {
            Prefix = prefix,
            Modules = ordered
        };
    }

    public int GetPageCount()
    {
        return View switch
        {
            HelpBrowserView.Modules => Math.Max(1, (int)Math.Ceiling((double)GetFilteredModules().Count / PageSize)),
            HelpBrowserView.ModuleCommands => Math.Max(1, (int)Math.Ceiling((double)(GetSelectedModule()?.Commands.Count ?? 0) / PageSize)),
            _ => 1
        };
    }

    public (int Start, int EndExclusive) GetPageSlice(int pageIndex)
    {
        var start = pageIndex * PageSize;
        var end = start + PageSize;
        return (start, end);
    }

    public ModuleHelpData? GetSelectedModule()
    {
        if (SelectedModuleIndex is null) return null;
        var index = SelectedModuleIndex.Value;
        return index >= 0 && index < Modules.Count ? Modules[index] : null;
    }

    public IReadOnlyList<CommandHelpData>? GetSelectedCommands()
    {
        var module = GetSelectedModule();
        return module?.Commands
            .OrderBy(c => c.Aliases.FirstOrDefault() ?? c.Name)
            .ToList();
    }

    public IReadOnlyList<(ModuleHelpData Module, int Index)> GetFilteredModules()
    {
        var filter = TagFilter?.Trim();
        if (string.IsNullOrWhiteSpace(filter))
            return Modules.Select((m, i) => (m, i)).ToList();

        return Modules
            .Select((m, i) => (m, i))
            .Where(x => x.m.HelpTags.Any(t => t.Equals(filter, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public IReadOnlyList<string> GetAvailableTags(int maxTags = 24)
    {
        var counts = Modules
            .SelectMany(m => m.HelpTags.Distinct(StringComparer.OrdinalIgnoreCase))
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Tag: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Tag, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Tag)
            .ToList();

        return counts.Take(Math.Max(0, maxTags)).ToList();
    }

    public bool TryApplyQuery(string? query)
    {
        query = query?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            View = HelpBrowserView.Modules;
            TagFilter = null;
            SelectedModuleIndex = null;
            SelectedCommandIndex = null;
            Notice = null;
            return true;
        }

        var tags = Modules
            .SelectMany(m => m.HelpTags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tagExact = tags.FirstOrDefault(t => t.Equals(query, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(tagExact))
        {
            View = HelpBrowserView.Modules;
            TagFilter = tagExact;
            SelectedModuleIndex = null;
            SelectedCommandIndex = null;
            Notice = null;
            return true;
        }

        var moduleExact = Modules
            .Select((m, i) => (m, i))
            .FirstOrDefault(x => x.m.Name.Equals(query, StringComparison.OrdinalIgnoreCase));

        if (moduleExact.m is not null)
        {
            View = HelpBrowserView.ModuleCommands;
            TagFilter = null;
            SelectedModuleIndex = moduleExact.i;
            SelectedCommandIndex = null;
            Notice = null;
            return true;
        }

        var moduleContains = Modules
            .Select((m, i) => (m, i))
            .FirstOrDefault(x => x.m.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (moduleContains.m is not null)
        {
            View = HelpBrowserView.ModuleCommands;
            TagFilter = null;
            SelectedModuleIndex = moduleContains.i;
            SelectedCommandIndex = null;
            Notice = null;
            return true;
        }

        var allCommands = Modules
            .SelectMany(m => m.Commands.Select(c => (Module: m, Command: c)))
            .ToList();

        var commandExact = allCommands.FirstOrDefault(x =>
            x.Command.Aliases.Any(a => a.Equals(query, StringComparison.OrdinalIgnoreCase)));

        if (commandExact.Command is not null)
            return SelectCommand(commandExact.Module, commandExact.Command);

        var commandContains = allCommands.FirstOrDefault(x =>
            x.Command.Aliases.Any(a => a.Contains(query, StringComparison.OrdinalIgnoreCase)));

        if (commandContains.Command is not null)
            return SelectCommand(commandContains.Module, commandContains.Command);

        Notice = $"No results for `{query}`.";
        View = HelpBrowserView.Modules;
        TagFilter = null;
        SelectedModuleIndex = null;
        SelectedCommandIndex = null;
        return false;

        bool SelectCommand(ModuleHelpData module, CommandHelpData command)
        {
            var moduleIndex = -1;
            for (var i = 0; i < Modules.Count; i++)
            {
                if (ReferenceEquals(Modules[i], module))
                {
                    moduleIndex = i;
                    break;
                }
            }

            if (moduleIndex < 0)
            {
                for (var i = 0; i < Modules.Count; i++)
                {
                    if (Modules[i].Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        moduleIndex = i;
                        break;
                    }
                }
            }

            if (moduleIndex < 0)
                return false;

            SelectedModuleIndex = moduleIndex;
            TagFilter = null;

            var ordered = module.Commands
                .OrderBy(c => c.Aliases.FirstOrDefault() ?? c.Name)
                .ToList();

            var index = ordered.FindIndex(c =>
                ReferenceEquals(c, command) ||
                c.Aliases.Intersect(command.Aliases, StringComparer.OrdinalIgnoreCase).Any());

            if (index < 0)
                return false;

            View = HelpBrowserView.CommandDetail;
            SelectedCommandIndex = index;
            Notice = null;
            return true;
        }
    }
}

