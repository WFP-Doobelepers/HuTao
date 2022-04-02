using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Fergun.Interactive;
using MediatR;
using Zhongli.Data.Config;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.AutoRemoveMessage;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Messages;
using MessageExtensions = Zhongli.Services.Utilities.MessageExtensions;

namespace Zhongli.Services.Quote;

public class MessageLinkBehavior :
    INotificationHandler<MessageReceivedNotification>
{
    private readonly AuthorizationService _auth;
    private readonly CommandErrorHandler _error;
    private readonly DiscordSocketClient _discordClient;
    private readonly InteractiveService _interactive;
    private readonly IQuoteService _quoteService;
    private readonly IRemovableMessageService _remove;

    public MessageLinkBehavior(
        AuthorizationService auth, CommandErrorHandler error,
        DiscordSocketClient discordClient, InteractiveService interactive,
        IQuoteService quoteService, IRemovableMessageService remove)
    {
        _auth          = auth;
        _error         = error;
        _discordClient = discordClient;
        _interactive   = interactive;
        _quoteService  = quoteService;
        _remove        = remove;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        => await OnMessageReceivedAsync(notification.Message, cancellationToken);

    private async Task OnMessageReceivedAsync(SocketMessage message, CancellationToken cancellationToken)
    {
        if (message is not SocketUserMessage source || message.Author.IsBot) return;
        if (message.Content?.StartsWith(ZhongliConfig.Configuration.Prefix) ?? true) return;

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
        var urls = MessageExtensions.GetJumpMessages(source.Content).Distinct().ToList();
        if (!urls.Any()) return;

        var paginator = await _quoteService.GetPaginatorAsync(context, urls);
        if (MessageExtensions.IsJumpUrls(source.Content))
            _ = source.DeleteAsync();

        if (paginator.MaxPageIndex > 0)
        {
            await _interactive.SendPaginatorAsync(paginator, source.Channel, cancellationToken: cancellationToken);
            return;
        }

        var page = await paginator.GetOrLoadCurrentPageAsync();
        var builders = page.Embeds.Select(e => e.ToEmbedBuilder()).ToList();
        await _remove.RegisterRemovableMessageAsync(context.User, builders, async embeds =>
        {
            return await source.Channel.SendMessageAsync(page.Text,
                embeds: embeds.Select(e => e.Build()).ToArray(),
                messageReference: source.Reference,
                allowedMentions: AllowedMentions.None);
        });
    }
}