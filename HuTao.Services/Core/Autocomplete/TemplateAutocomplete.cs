using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.Autocomplete;

public class TemplateAutocomplete : AuthorizedAutocompleteHandler
{
    protected override async Task<AutocompletionResult> GetSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var auth = services.GetRequiredService<AuthorizationService>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        // The template command has no precondition and checks each template's own scope when it runs,
        // so only suggest the templates the user may apply, unless they may configure templates.
        var user = new InteractionContext(context);
        var configure = await auth.IsAuthorizedAsync(user, AuthorizationScope.All | AuthorizationScope.Configuration);

        var input = interaction.Data.Current.Value.ToString();
        var templates = new List<AutocompleteResult>();
        foreach (var t in guild.ModerationTemplates
            .Where(t => string.IsNullOrEmpty(input) || t.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase)))
        {
            if (!configure && !await auth.IsAuthorizedAsync(user, t.Scope))
                continue;

            templates.Add(new AutocompleteResult(
                $"{t.Name}: [{(t.Action as IAction)?.CleanAction}] with reason [{t.Reason}]".Truncate(100), t.Name));
        }

        return AutocompletionResult.FromSuccess(templates);
    }
}