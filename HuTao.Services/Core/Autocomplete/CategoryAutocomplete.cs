using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.Autocomplete;

public class CategoryAutocomplete : AuthorizedAutocompleteHandler
{
    protected override async Task<AutocompletionResult> GetSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var input = interaction.Data.Current.Value.ToString();
        var categories = guild.ModerationCategories
            .Prepend(ModerationCategory.None)
            .Append(ModerationCategory.All)
            .Where(t => t.Name.StartsWith(input ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        // The command checks the category with the parameter's own preconditions, so only suggest those that pass.
        var suggestions = new List<AutocompleteResult>();
        foreach (var category in categories)
        {
            var result = await parameter.CheckPreconditionsAsync(context, category, services);
            if (result.IsSuccess)
                suggestions.Add(new AutocompleteResult(category.Name.Truncate(100), category.Name));
        }

        return AutocompletionResult.FromSuccess(suggestions);
    }
}