using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.Autocomplete;

public class ReprimandAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var input = interaction.Data.Current.Value.ToString();
        var reprimands = guild.ReprimandHistory
            .Where(r => r.Id.ToString().StartsWith(input ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Action?.Date).Take(25)
            .Select(r => new AutocompleteResult($"{r.GetTitle(true)} {r.GetReason()}".Truncate(100), r.Id.ToString()));

        return AutocompletionResult.FromSuccess(reprimands);
    }
}