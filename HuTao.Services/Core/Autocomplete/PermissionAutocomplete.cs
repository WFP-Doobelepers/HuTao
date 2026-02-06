using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.Autocomplete;

public class PermissionAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var input = interaction.Data.Current.Value.ToString() ?? string.Empty;
        var groups = guild.AuthorizationGroups
            .Where(g => g.Id.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
                        g.Scope.ToString().Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(g => new AutocompleteResult(
                $"{g.Scope.Humanize()} [{g.Access}] ({g.JudgeType})".Truncate(100),
                g.Id.ToString()));

        return AutocompletionResult.FromSuccess(groups);
    }
}
