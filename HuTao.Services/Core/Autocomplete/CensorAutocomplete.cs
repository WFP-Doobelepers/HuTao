using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.Autocomplete;

public class CensorAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);
        guild.ModerationRules ??= new ModerationRules();

        var input = interaction.Data.Current.Value.ToString() ?? string.Empty;
        var censors = guild.ModerationRules.Triggers.OfType<Censor>()
            .Where(c => c.Id.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
                        c.Pattern.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(c => new AutocompleteResult(
                $"{c.Pattern} [{(c.IsActive ? "Active" : "Inactive")}]".Truncate(100),
                c.Id.ToString()));

        return AutocompletionResult.FromSuccess(censors);
    }
}
