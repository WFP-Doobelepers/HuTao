using System;
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

public class VariableAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);
        guild.ModerationRules ??= new ModerationRules();

        var input = interaction.Data.Current.Value.ToString() ?? string.Empty;
        var variables = guild.ModerationRules.Variables
            .Where(v => v.Id.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
                        v.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(v => new AutocompleteResult(
                $"{v.Name} = {v.Value}".Truncate(100),
                v.Id.ToString()));

        return AutocompletionResult.FromSuccess(variables);
    }
}
