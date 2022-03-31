﻿using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;
using Humanizer;

namespace Zhongli.Services.CommandHelp;

public class ModuleHelpData
{
    public IReadOnlyCollection<CommandHelpData> Commands { get; set; } = null!;

    public IReadOnlyCollection<string> HelpTags { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Summary { get; set; }

    public ulong? GuildId { get; set; }

    public static ModuleHelpData FromModuleInfo(ModuleInfo module)
    {
        var moduleName = module.Name;

        var suffixPosition = moduleName.IndexOf("Module", StringComparison.Ordinal);
        if (suffixPosition > -1) moduleName = module.Name[..suffixPosition].Humanize();

        moduleName = moduleName.ApplyCase(LetterCasing.Title);

        var ret = new ModuleHelpData
        {
            Name    = moduleName,
            Summary = string.IsNullOrWhiteSpace(module.Summary) ? "No Summary." : module.Summary,
            Commands = module.Commands
                .Where(x => !ShouldBeHidden(x))
                .Select(CommandHelpData.FromCommandInfo)
                .ToArray(),
            HelpTags = module.Attributes
                    .OfType<HelpTagsAttribute>()
                    .SingleOrDefault()
                    ?.Tags
                ?? Array.Empty<string>()
        };

        return ret;

        static bool ShouldBeHidden(CommandInfo command) =>
            command.Preconditions.Any(x => x is RequireOwnerAttribute)
            || command.Attributes.Any(x => x is HiddenFromHelpAttribute);
    }
}