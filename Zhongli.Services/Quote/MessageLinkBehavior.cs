using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Logging;
using Zhongli.Data.Config;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;
using MessageExtensions = Zhongli.Services.Utilities.MessageExtensions;

namespace Zhongli.Services.Quote;

public class MessageLinkBehavior :
    INotificationHandler<MessageReceivedNotification>,
    INotificationHandler<MessageUpdatedNotification>
{
    private readonly AuthorizationService _auth;
    private readonly DiscordSocketClient _discordClient;
    private readonly ILogger<MessageLinkBehavior> _log;
    private readonly IQuoteService _quoteService;

    public MessageLinkBehavior(
        AuthorizationService auth, DiscordSocketClient discordClient,
        IQuoteService quoteService, ILogger<MessageLinkBehavior> log)
    {
        _auth          = auth;
        _discordClient = discordClient;
        _quoteService  = quoteService;
        _log           = log;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken) { await OnMessageReceivedAsync(notification.Message, cancellationToken); }

    public async Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var cachedMessage = await notification.OldMessage.GetOrDownloadAsync();

        if (cachedMessage is null)
            return;

        if (RegexUtilities.JumpUrl.IsMatch(cachedMessage.Content))
            return;

        await OnMessageReceivedAsync(notification.NewMessage, cancellationToken);
    }

    private async Task OnMessageReceivedAsync(SocketMessage message, CancellationToken cancellationToken)
    {
        if (message.Content?.StartsWith(ZhongliConfig.Configuration.Prefix) ?? true) return;
        if (message is not SocketUserMessage userMessage || message.Author.IsBot)
            return;

        var context = new SocketCommandContext(_discordClient, userMessage);
        if (!await _auth.IsAuthorizedAsync(context, AuthorizationScope.Quote, cancellationToken))
            return;

        foreach (Match match in RegexUtilities.JumpUrl.Matches(message.Content))
        {
            // check if the link is surrounded with < and >. This was too annoying to do in regex
            if (match.Groups["OpenBrace"].Success && match.Groups["CloseBrace"].Success)
                continue;

            if (!MessageExtensions.TryGetJumpUrl(match, out _, out var channelId, out var messageId))
                continue;

            try
            {
                var channel = _discordClient.GetChannel(channelId);
                if (channel is not ITextChannel textChannel || textChannel.IsNsfw) return;

                var user = await textChannel.Guild.GetUserAsync(message.Author.Id);
                var channelPermissions = user.GetPermissions(textChannel);
                if (!channelPermissions.ViewChannel) return;

                var cacheMode = channelPermissions.ReadMessageHistory
                    ? CacheMode.AllowDownload
                    : CacheMode.CacheOnly;

                var quote = await textChannel.GetMessageAsync(messageId, cacheMode);
                if (quote is null) return;

                var success = await SendQuoteEmbedAsync(message, quote);
                if (success
                    && string.IsNullOrEmpty(match.Groups["Prelink"].Value)
                    && string.IsNullOrEmpty(match.Groups["Postlink"].Value))
                    await userMessage.DeleteAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred while attempting to create a quote embed");
            }
        }
    }

    private async Task<bool> SendQuoteEmbedAsync(SocketMessage source, IMessage quote)
    {
        var success = false;
        await _quoteService.BuildRemovableEmbed(quote, source.Author,
            async embed => //If embed building is unsuccessful, this won't execute
            {
                success = true;

                return await source.Channel.SendMessageAsync(
                    embed: embed.Build(),
                    messageReference: source.Reference,
                    allowedMentions: AllowedMentions.None);
            });

        return success;
    }
}