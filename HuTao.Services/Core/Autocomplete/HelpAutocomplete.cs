using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Services.CommandHelp;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.Autocomplete;

public class HelpAutocomplete : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var help = services.GetRequiredService<ICommandHelpService>();
        var input = interaction.Data.Current.Value?.ToString() ?? string.Empty;

        var modules = help.GetModuleHelpData();
        var moduleResults = modules
            .Where(m => m.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase)
                || m.HelpTags.Any(t => t.StartsWith(input, StringComparison.OrdinalIgnoreCase)))
            .Select(m => new AutocompleteResult($"Module: {m.Name}".Truncate(100), m.Name));

        var commandResults = modules
            .SelectMany(m => m.Commands)
            .SelectMany(c => c.Aliases.Select(a => (Alias: a, Command: c)))
            .Where(x => x.Alias.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
            .Select(x => new AutocompleteResult($"Command: {x.Alias}".Truncate(100), x.Alias));

        var combined = moduleResults
            .Concat(commandResults)
            .Take(25)
            .ToList();

        return Task.FromResult(AutocompletionResult.FromSuccess(combined));
    }
}

