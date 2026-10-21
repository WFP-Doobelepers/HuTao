using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using HuTao.Services.Quote;
using HuTao.Services.Utilities;
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
        if (!await CanReadSourceAsync(Context.User as IGuildUser, Context.Guild.GetChannel(channelId)))
        {
            await FollowupAsync("You cannot read the channel this quote comes from.", ephemeral: true);
            return;
        }

        // The mode is "1" or "0", numbered after a dot ("1.2") so that two buttons in one quote never share an id.
        var expanded = mode.Split('.')[0] == "1";
        var messages = await quoteService.BuildFullQuoteAsync(Context.Guild, channelId, messageId, expanded);
        if (messages.Count == 0)
        {
            await FollowupAsync("Could not load the quote.", ephemeral: true);
            return;
        }

        foreach (var components in messages)
            await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: true);
    }

    /// <summary>
    ///     Whether <paramref name="user" /> can see <paramref name="channel" /> and read its history. A thread has no
    ///     permissions of its own, so its parent channel decides, and a private thread is also hidden from everyone who
    ///     is not in it, unless they can manage threads.
    /// </summary>
    public static async Task<bool> CanReadSourceAsync(IGuildUser? user, IGuildChannel? channel)
    {
        if (user is null || channel is null) return false;
        if (channel is not IThreadChannel thread) return CanRead(user, channel);

        var parent = thread.CategoryId is { } parentId ? await user.Guild.GetChannelAsync(parentId) : null;
        if (parent is null || !CanRead(user, parent)) return false;
        if (thread.Type is not ThreadType.PrivateThread) return true;

        return user.GetPermissions(parent).ManageThreads || await ThreadMembers.IsMemberAsync(thread, user.Id);

        static bool CanRead(IGuildUser user, IGuildChannel channel)
            => user.GetPermissions(channel) is { ViewChannel: true, ReadMessageHistory: true };
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

        // Expanding pulls the messages around the quote out of the source channel into a public message, so only
        // people who can read that channel may do it. Collapsing shows less and stays open to everyone.
        if (expanded && !await CanReadSourceAsync(Context.User as IGuildUser, Context.Guild.GetChannel(channelId)))
        {
            await RespondAsync("You cannot read the channel this quote comes from.", ephemeral: true);
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
