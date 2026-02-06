using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.Autocomplete;

public class AutoConfigAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);
        guild.ModerationRules ??= new ModerationRules();

        var input = interaction.Data.Current.Value.ToString() ?? string.Empty;
        var configs = guild.ModerationRules.Triggers
            .OfType<AutoConfiguration>()
            .Where(c => c.Id.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase)
                || c.GetType().Name.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(c =>
            {
                var name = $"{c.GetType().Name.Replace("Configuration", "")} ({c.Amount}/{c.Length.Humanize()})".Truncate(100);
                return new AutocompleteResult(name, c.Id.ToString());
            });

        return AutocompletionResult.FromSuccess(configs);
    }
}
