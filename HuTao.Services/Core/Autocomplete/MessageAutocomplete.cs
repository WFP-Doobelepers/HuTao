using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Services.Core.TypeReaders.Interactions;

namespace HuTao.Services.Core.Autocomplete;

public class MessageAutocomplete<T> : AutocompleteHandler where T : IMessage
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var reader = new UserTypeReader<IUser>();
        var input = interaction.Data.Options.FirstOrDefault(o => o.Type is ApplicationCommandOptionType.User);
        var user = await reader.ReadAsync(context, input?.Value.ToString() ?? string.Empty, services);
        var messages = await context.Channel.GetMessagesAsync().FlattenAsync();

        return AutocompletionResult.FromSuccess(messages.OfType<T>()
            .Where(m => !user.IsSuccess || (user.IsSuccess && user.Value is IUser u && m.Author == u))
            .Where(m => m.Content.StartsWith(interaction.Data.Current.Value.ToString() ?? string.Empty))
            .Take(25).Select(m => new AutocompleteResult(
                $"[{m.Timestamp.Humanize()}] {m.Author}: {m.Content}".Truncate(100), m.GetJumpUrl())));
    }
}