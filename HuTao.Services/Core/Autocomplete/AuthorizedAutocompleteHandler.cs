using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace HuTao.Services.Core.Autocomplete;

/// <summary>
///     Discord.Net runs autocomplete handlers without the preconditions of the command they belong to, so anyone could
///     read the suggestions of a command they may not use. This runs those preconditions first and suggests nothing
///     when they fail.
/// </summary>
public abstract class AuthorizedAutocompleteHandler : AutocompleteHandler
{
    public sealed override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var preconditions = await parameter.Command.CheckPreconditionsAsync(context, services);

        return preconditions.IsSuccess
            ? await GetSuggestionsAsync(context, interaction, parameter, services)
            : AutocompletionResult.FromSuccess([]);
    }

    protected abstract Task<AutocompletionResult> GetSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services);
}
