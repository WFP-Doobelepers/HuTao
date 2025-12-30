using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Fergun.Interactive;
using HuTao.Data.Config;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Services.Core;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Messages;
using MediatR;
using MessageExtensions = HuTao.Services.Utilities.MessageExtensions;

namespace HuTao.Services.Quote;

public class MessageLinkBehavior(
    AuthorizationService auth,
    CommandErrorHandler error,
    DiscordSocketClient discordClient,
    InteractiveService interactive,
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
            await SendQuoteEmbedAsync(context, source, cancellationToken);
        }
        catch (HttpException ex)
        {
            await error.AssociateError(context, ex.Message);
        }
    }

    private async Task SendQuoteEmbedAsync(
        Context context, SocketMessage source,
        CancellationToken cancellationToken)
    {
        var urls = MessageExtensions.GetJumpMessages(source.Content).ToList();
        if (!urls.Any()) return;

        var paginator = await quoteService.GetPaginatorAsync(context, source, urls);
        if (paginator is null) return;

        if (MessageExtensions.IsJumpUrls(source.Content)) _ = source.DeleteAsync();

        await interactive.SendPaginatorAsync(paginator, source.Channel,
            cancellationToken: cancellationToken,
            resetTimeoutOnInput: true);
    }
}