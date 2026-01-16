using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.Autocomplete;

public class SlashCommandAutocomplete : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var commands = services.GetRequiredService<InteractionService>();
        var input = interaction.Data.Current.Value?.ToString() ?? string.Empty;

        var results = commands.SlashCommands
            .Select(c => new
            {
                FullName = GetFullName(c),
                c.Description
            })
            .Where(x => string.IsNullOrWhiteSpace(input)
                || x.FullName.StartsWith(input, StringComparison.OrdinalIgnoreCase)
                || x.FullName.Contains(input, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.FullName)
            .Take(25)
            .Select(x => new AutocompleteResult($"{x.FullName}: {x.Description}".Truncate(100), x.FullName))
            .ToList();

        return Task.FromResult(AutocompletionResult.FromSuccess(results));
    }

    private static string GetFullName(SlashCommandInfo command)
    {
        var parts = new Stack<string>();
        parts.Push(command.Name);

        for (var module = command.Module; module is not null; module = module.Parent)
        {
            if (!string.IsNullOrWhiteSpace(module.SlashGroupName))
                parts.Push(module.SlashGroupName);
        }

        return string.Join(' ', parts);
    }
}

