using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
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

        // The commands check each reprimand's own category when they run, so only suggest the reprimands the user may
        // view. The history is the whole guild's, so decide once per category.
        var user = new InteractionContext(context);
        var categories = new Dictionary<ModerationCategory, bool>();

        var input = interaction.Data.Current.Value.ToString();
        var reprimands = new List<AutocompleteResult>();
        foreach (var r in guild.ReprimandHistory
            .Where(r => r.Id.ToString().StartsWith(input ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Action?.Date))
        {
            var category = r.Category ?? ModerationCategory.None;
            if (!categories.TryGetValue(category, out var authorized))
                categories[category] = authorized = await auth.IsCategoryAuthorizedAsync(user, Scope, category);

            if (!authorized)
                continue;

            reprimands.Add(new AutocompleteResult($"{r.GetTitle(true)} {r.GetLatestReason()}".Truncate(100),
                r.Id.ToString()));

            if (reprimands.Count == 25)
                break;
        }

        return AutocompletionResult.FromSuccess(reprimands);
    }
}