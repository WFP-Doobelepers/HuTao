using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Core.Preconditions.Interactions;

namespace HuTao.Bot.Modules.Moderation;

[RequireContext(ContextType.Guild)]
public class InteractivePurgeModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("purge", "Purge messages from the current channel.")]
    [RequireAuthorization(AuthorizationScope.Purge)]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    [RequireBotPermission(ChannelPermission.ManageMessages)]
    public async Task PurgeAsync(
        [Summary(description: "Number of messages to purge")]
        int amount,
        [Summary(description: "Only messages from this user")]
        IUser? user = null,
        [Summary(description: "Only messages containing this text")]
        string? contains = null,
        [Summary(description: "Only messages from bots")]
        bool? isBot = null,
        [Summary(description: "Only messages with attachments")]
        bool? hasAttachments = null,
        [RequireEphemeralScope] bool ephemeral = true)
    {
        await DeferAsync(ephemeral);

        var messages = await Context.Channel
            .GetMessagesAsync(amount + 1, CacheMode.AllowDownload)
            .Flatten().ToListAsync();

        messages = messages
            .Where(m => m.Id != Context.Interaction.Id)
            .Take(amount)
            .ToList();

        var filtered = messages.AsEnumerable();

        if (user is not null)
            filtered = filtered.Where(m => m.Author.Id == user.Id);
        if (contains is not null)
            filtered = filtered.Where(m => m.Content.Contains(contains, StringComparison.OrdinalIgnoreCase));
        if (isBot is not null)
            filtered = filtered.Where(m => (m.Author.IsBot || m.Author.IsWebhook) == isBot.Value);
        if (hasAttachments is not null)
            filtered = filtered.Where(m => m.Attachments.Any() == hasAttachments.Value);

        var toDelete = filtered.ToList();
        var channel = (ITextChannel)Context.Channel;
        await channel.DeleteMessagesAsync(toDelete);

        var components = new ComponentBuilderV2()
            .WithContainer(new ContainerBuilder()
                .WithTextDisplay($"## Purge\nDeleted **{toDelete.Count}** messages.")
                .WithAccentColor(0x9B59FF))
            .Build();

        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }
}
