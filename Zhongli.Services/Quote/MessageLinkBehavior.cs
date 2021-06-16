using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Logging;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.Quote
{
    public class MessageLinkBehavior :
        INotificationHandler<MessageReceivedNotification>,
        INotificationHandler<MessageUpdatedNotification>
    {
        private const string PatternString =
            @"(?<Prelink>\S+\s+\S*)?(?<OpenBrace><)?https?://(?:(?:ptb|canary)\.)?discord(app)?\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)/?(?<CloseBrace>>)?(?<Postlink>\S*\s+\S+)?";

        private static readonly Regex Pattern =
            new(PatternString, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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

            if (Pattern.IsMatch(cachedMessage.Content))
                return;

            await OnMessageReceivedAsync(notification.NewMessage);
        }

        private async Task OnMessageReceivedAsync(IMessage message)
        {
            if (message is not IUserMessage { Author: IGuildUser guildUser } userMessage)
                return;

            if (guildUser.IsBot || guildUser.IsWebhook)
                return;

            foreach (Match match in Pattern.Matches(message.Content))
            {
                // check if the link is surrounded with < and >. This was too annoying to do in regex
                if (match.Groups["OpenBrace"].Success && match.Groups["CloseBrace"].Success)
                    continue;

                if (ulong.TryParse(match.Groups["GuildId"].Value, out var guildId)
                    && ulong.TryParse(match.Groups["ChannelId"].Value, out var channelId)
                    && ulong.TryParse(match.Groups["MessageId"].Value, out var messageId))
                    try
                    {
                        var channel = _discordClient.GetChannel(channelId);

                        if (channel is ITextChannel { IsNsfw: true }) return;

                        if (channel is IGuildChannel guildChannel and ISocketMessageChannel messageChannel)
                        {
                            var currentUser = await guildChannel.Guild.GetCurrentUserAsync();
                            var channelPermissions = currentUser.GetPermissions(guildChannel);

                            if (!channelPermissions.ViewChannel) return;

                            var cacheMode = channelPermissions.ReadMessageHistory
                                ? CacheMode.AllowDownload
                                : CacheMode.CacheOnly;

                            var msg = await messageChannel.GetMessageAsync(messageId, cacheMode);

                            if (msg is null) return;

                            var success = await SendQuoteEmbedAsync(msg, guildUser, userMessage.Channel);
                            if (success
                                && string.IsNullOrEmpty(match.Groups["Prelink"].Value)
                                && string.IsNullOrEmpty(match.Groups["Postlink"].Value))
                                await userMessage.DeleteAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "An error occurred while attempting to create a quote embed.");
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