using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Rest;
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

public class MessageLinkBehavior :
    INotificationHandler<MessageReceivedNotification>
{
    private readonly AuthorizationService _auth;
    private readonly CommandErrorHandler _error;
    private readonly DiscordSocketClient _discordClient;
    private readonly InteractiveService _interactive;
    private readonly IQuoteService _quoteService;

    public MessageLinkBehavior(
        AuthorizationService auth, CommandErrorHandler error,
        DiscordSocketClient discordClient, InteractiveService interactive,
        IQuoteService quoteService)
    {
        _auth          = auth;
        _error         = error;
        _discordClient = discordClient;
        _interactive   = interactive;
        _quoteService  = quoteService;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        => await OnMessageReceivedAsync(notification.Message, cancellationToken);

    private async Task OnMessageReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        if (message is not SocketUserMessage { Author.IsBot: false } source) return;
        if (message.Content?.StartsWith(HuTaoConfig.Configuration.Prefix) ?? true) return;

        var context = (Context) new SocketCommandContext(_discordClient, source);
        if (!await _auth.IsAuthorizedAsync(context, AuthorizationScope.Quote, cancellationToken))
            return;

        try
        {
            await SendQuoteEmbedAsync(context, source, cancellationToken);
        }
        catch (HttpException ex)
        {
            await _error.AssociateError(context, ex.Message);
        }
    }

    private async Task SendQuoteEmbedAsync(
        Context context, SocketMessage source,
        CancellationToken cancellationToken)
    {
        var urls = MessageExtensions.GetJumpMessages(source.Content).ToList();
        if (!urls.Any()) return;

        var paginator = await _quoteService.GetPaginatorAsync(context, source, urls);
        if (paginator is null) return;

        var page = await paginator.GetOrLoadCurrentPageAsync() as QuotedPage;
        if (MessageExtensions.IsJumpUrls(source.Content)) _ = source.DeleteAsync();

        await _interactive.SendPaginatorAsync(paginator, source.Channel,
            cancellationToken: cancellationToken,
            messageAction: m => MessageAction(m, page?.Quote));
    }

    private static void MessageAction(IUserMessage message, QuotedMessage? quoted)
    {
        if (message.Components.Any() || quoted is null) return;
        var components = new ComponentBuilder().WithQuotedMessage(quoted).Build();

        _ = message switch
        {
            RestInteractionMessage m => m.ModifyAsync(r => r.Components       = components),
            RestFollowupMessage m    => m.ModifyAsync(r => r.Components       = components),
            _                        => message.ModifyAsync(r => r.Components = components)
        };
    }
}