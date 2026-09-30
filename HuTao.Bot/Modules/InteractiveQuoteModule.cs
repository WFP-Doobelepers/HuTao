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

    /// <summary>
    ///     A quote that had to leave something out has a "Show all" button. It sends the whole quote, over as many
    ///     messages as it needs, only to the person who pressed it.
    /// </summary>
    [ComponentInteraction("quote:all:*:*:*")]
    public async Task ShowAllAsync(string channelIdStr, string messageIdStr, string mode)
    {
        if (!ulong.TryParse(channelIdStr, out var channelId) || !ulong.TryParse(messageIdStr, out var messageId))
        {
            await RespondAsync("Invalid quote reference.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // The whole quote shows more of the source channel than the posted one, so only people who can read that
        // channel get it.
        var channel = Context.Guild.GetChannel(channelId);
        if (channel is null || Context.User is not SocketGuildUser user
            || user.GetPermissions(channel) is not { ViewChannel: true, ReadMessageHistory: true })
        {
            await FollowupAsync("You cannot read the channel this quote comes from.", ephemeral: true);
            return;
        }

        var messages = await quoteService.BuildFullQuoteAsync(Context.Guild, channelId, messageId, mode == "1");
        if (messages.Count == 0)
        {
            await FollowupAsync("Could not load the quote.", ephemeral: true);
            return;
        }

        foreach (var components in messages)
            await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: true);
    }

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
