using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.Autocomplete;

public class TemplateAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var input = interaction.Data.Current.Value.ToString();
        var templates = guild.ModerationTemplates
            .Where(t => string.IsNullOrEmpty(input) || t.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .Select(t => new AutocompleteResult(
                $"{t.Name}: [{(t.Action as IAction)?.CleanAction}] with reason [{t.Reason}]".Truncate(100), t.Name));

        return AutocompletionResult.FromSuccess(templates);
    }
}