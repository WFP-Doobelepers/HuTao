using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Logging;
using Zhongli.Data.Config;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;
using MessageExtensions = Zhongli.Services.Utilities.MessageExtensions;

namespace Zhongli.Services.Quote
{
    public class MessageLinkBehavior :
        INotificationHandler<MessageReceivedNotification>,
        INotificationHandler<MessageUpdatedNotification>
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly ILogger<MessageLinkBehavior> _log;
        private readonly IQuoteService _quoteService;

        public MessageLinkBehavior(DiscordSocketClient discordClient,
            IQuoteService quoteService, ILogger<MessageLinkBehavior> log)
        {
            _discordClient = discordClient;
            _quoteService  = quoteService;

            _log = log;
        }

        public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            await OnMessageReceivedAsync(notification.Message);
        }

        public async Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
        {
            var cachedMessage = await notification.OldMessage.GetOrDownloadAsync();

            if (cachedMessage is null)
                return;

            if (MessageExtensions.JumpUrlRegex.IsMatch(cachedMessage.Content))
                return;

            await OnMessageReceivedAsync(notification.NewMessage);
        }

        private async Task OnMessageReceivedAsync(IMessage message)
        {
            if (message.Content?.StartsWith(ZhongliConfig.Configuration.Prefix) ?? true) return;

            if (message is not IUserMessage { Author: IGuildUser guildUser } userMessage)
                return;

            if (guildUser.IsBot || guildUser.IsWebhook)
                return;

            foreach (Match match in MessageExtensions.JumpUrlRegex.Matches(message.Content))
            {
                // check if the link is surrounded with < and >. This was too annoying to do in regex
                if (match.Groups["OpenBrace"].Success && match.Groups["CloseBrace"].Success)
                    continue;

                if (!ulong.TryParse(match.Groups["GuildId"].Value, out _) ||
                    !ulong.TryParse(match.Groups["ChannelId"].Value, out var channelId) ||
                    !ulong.TryParse(match.Groups["MessageId"].Value, out var messageId)) continue;

                try
                {
                    var msg = await _discordClient.GetMessageAsync(channelId, messageId);

                    if (msg is null) return;

                    var success = await SendQuoteEmbedAsync(msg, guildUser, userMessage.Channel);
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

        private async Task<bool> SendQuoteEmbedAsync(IMessage message, IUser quoter, IMessageChannel targetChannel)
        {
            var success = false;
            await _quoteService.BuildRemovableEmbed(message, quoter,
                async embed => //If embed building is unsuccessful, this won't execute
                {
                    success = true;
                    return await targetChannel.SendMessageAsync(embed: embed.Build());
                });

            return success;
        }
    }
}