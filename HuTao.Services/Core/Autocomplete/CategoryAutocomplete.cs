using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.Autocomplete;

public class CategoryAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        await using var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var input = interaction.Data.Current.Value.ToString();
        var templates = guild.ModerationCategories
            .Append(ModerationCategory.None)
            .Where(t => string.IsNullOrEmpty(input) || t.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .Where(t => AuthorizationService.IsAuthorized(new InteractionContext(context), t.Authorization))
            .Select(t => new AutocompleteResult(t.Name.Truncate(100), t.Name));

        return AutocompletionResult.FromSuccess(templates);
    }
}