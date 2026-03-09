using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using HuTao.Data.Config;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Services.Core;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;
using Serilog;
using MessageExtensions = HuTao.Services.Utilities.MessageExtensions;

namespace HuTao.Services.Quote;

public class MessageLinkBehavior(
    AuthorizationService auth,
    CommandErrorHandler error,
    DiscordSocketClient discordClient,
    IQuoteService quoteService)
    : INotificationHandler<MessageReceivedNotification>
{
    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        => await OnMessageReceivedAsync(notification.Message, cancellationToken);

    private async Task OnMessageReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        if (message is not SocketUserMessage { Author.IsBot: false } source) return;
        if (message.Content?.StartsWith(HuTaoConfig.Configuration.Prefix) ?? true) return;

        var context = (Context) new SocketCommandContext(discordClient, source);
        if (!await auth.IsAuthorizedAsync(context, AuthorizationScope.Quote, cancellationToken))
            return;

        try
        {
            await SendQuoteAsync(context, source);
        }
        catch (HttpException ex) when (ex.DiscordCode != DiscordErrorCode.UnknownMessage)
        {
            try { await error.AssociateError(context, ex.Message); }
            catch (HttpException) { }
        }
    }

    private async Task SendQuoteAsync(Context context, SocketMessage source)
    {
        var urls = MessageExtensions.GetJumpMessages(source.Content).ToList();
        if (!urls.Any()) return;

        Log.Debug("[Quote] Processing {Count} jump URL(s) from {UserId}", urls.Count, source.Author.Id);

        var messages = await quoteService.BuildQuoteAsync(context, context.User, urls);
        if (messages.Count == 0)
        {
            Log.Warning("[Quote] BuildQuoteAsync returned empty");
            return;
        }

        Log.Debug("[Quote] Sending {Count} quote message(s)", messages.Count);

        if (MessageExtensions.IsJumpUrls(source.Content)) source.DeleteAsync().SafeFireAndForget();

        var allowedMentions = new AllowedMentions(AllowedMentionTypes.None) { MentionRepliedUser = true };

        try
        {
            for (var i = 0; i < messages.Count; i++)
            {
                await source.Channel.SendMessageAsync(
                    components: messages[i],
                    allowedMentions: allowedMentions,
                    messageReference: i == 0 ? source.Reference : null);
            }

            Log.Debug("[Quote] Quote sent successfully ({Count} messages)", messages.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Quote] Failed to send quote");
        }
    }
}
