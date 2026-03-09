using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using HuTao.Services.Quote;
using Serilog;

namespace HuTao.Bot.Modules;

public class InteractiveQuoteModule(IQuoteService quoteService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("quote:expand:*:*:*")]
    public Task ExpandQuoteAsync(string channelId, string messageId, string requesterId)
        => ToggleQuoteAsync(channelId, messageId, requesterId, expanded: true);

    [ComponentInteraction("quote:collapse:*:*:*")]
    public Task CollapseQuoteAsync(string channelId, string messageId, string requesterId)
        => ToggleQuoteAsync(channelId, messageId, requesterId, expanded: false);

    private async Task ToggleQuoteAsync(
        string channelIdStr, string messageIdStr, string requesterIdStr, bool expanded)
    {
        if (!ulong.TryParse(channelIdStr, out var channelId)
            || !ulong.TryParse(messageIdStr, out var messageId)
            || !ulong.TryParse(requesterIdStr, out var requesterId))
        {
            await RespondAsync("Invalid quote reference.", ephemeral: true);
            return;
        }

        var interaction = (IComponentInteraction) Context.Interaction;
        await DeferAsync();

        var requester = Context.Guild.GetUser(requesterId)
            ?? await Context.Client.Rest.GetUserAsync(requesterId) as IUser
            ?? Context.User;

        var messages = await quoteService.RebuildQuoteAsync(
            Context.Guild, requester, channelId, messageId, expanded);

        if (messages.Count == 0)
        {
            await FollowupAsync("Could not rebuild quote.", ephemeral: true);
            return;
        }

        Log.Debug("[Quote] Rebuilding quote {MessageId} (expanded={Expanded}, containers={Count})",
            messageId, expanded, messages.Count);

        var allowedMentions = new AllowedMentions(AllowedMentionTypes.None) { MentionRepliedUser = true };
        await interaction.Message.ModifyAsync(m =>
        {
            m.Components = new Optional<MessageComponent>(messages[^1]);
            m.AllowedMentions = allowedMentions;
        });
    }
}
