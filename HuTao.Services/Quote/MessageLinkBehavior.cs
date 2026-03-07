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
        catch (HttpException ex)
        {
            await error.AssociateError(context, ex.Message);
        }
    }

    private async Task SendQuoteAsync(Context context, SocketMessage source)
    {
        var urls = MessageExtensions.GetJumpMessages(source.Content).ToList();
        if (!urls.Any()) return;

        var components = await quoteService.BuildQuoteAsync(context, context.User, urls);
        if (components is null) return;

        if (MessageExtensions.IsJumpUrls(source.Content)) source.DeleteAsync().SafeFireAndForget();

        var allowedMentions = new AllowedMentions(AllowedMentionTypes.None) { MentionRepliedUser = true };

        await source.Channel.SendMessageAsync(
            components: components,
            allowedMentions: allowedMentions,
            messageReference: source.Reference);
    }
}
