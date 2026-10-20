using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.Autocomplete;

public class ReprimandAutocomplete : AuthorizedAutocompleteHandler
{
    private const AuthorizationScope Scope = AuthorizationScope.All | AuthorizationScope.History;

    protected override async Task<AutocompletionResult> GetSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var auth = services.GetRequiredService<AuthorizationService>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var input = interaction.Data.Current.Value.ToString();
        var matches = guild.ReprimandHistory
            .Where(r => r.Id.ToString().StartsWith(input ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Action?.Date);

        // The commands check each reprimand's own category when they run, so only suggest the reprimands the user may
        // view.
        var visible = await auth.WhereReprimandsAuthorizedAsync(new InteractionContext(context), Scope, matches);
        var reprimands = visible.Take(25)
            .Select(r => new AutocompleteResult($"{r.GetTitle(true)} {r.GetLatestReason()}".Truncate(100),
                r.Id.ToString()));

        return AutocompletionResult.FromSuccess(reprimands);
    }
}