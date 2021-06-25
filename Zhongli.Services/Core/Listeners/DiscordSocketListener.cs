using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.Core.Listeners
{
    /// <summary>
    ///     Listens for events from an <see cref="DiscordSocketClient" /> and dispatches them to the rest of the application,
    ///     through an <see cref="MessageDispatcher" />.
    /// </summary>
    public class DiscordSocketListener
    {
        private CancellationToken _cancellationToken;

        /// <summary>
        ///     Constructs a new <see cref="DiscordSocketListener" /> with the given dependencies.
        /// </summary>
        public DiscordSocketListener(
            DiscordSocketClient discordSocketClient,
            IMediator messageDispatcher)
        {
            DiscordSocketClient = discordSocketClient;
            MessageDispatcher   = messageDispatcher;
        }

        /// <summary>
        ///     The <see cref="DiscordSocketClient" /> to be listened to.
        /// </summary>
        private DiscordSocketClient DiscordSocketClient { get; }

        /// <summary>
        ///     A <see cref="IMessageDispatcher" /> used to dispatch discord notifications to the rest of the application.
        /// </summary>
        private IMediator MessageDispatcher { get; }

        public Task StartAsync(
            CancellationToken cancellationToken)
        {
            _cancellationToken                  =  cancellationToken;
            DiscordSocketClient.ChannelCreated  += OnChannelCreatedAsync;
            DiscordSocketClient.ChannelUpdated  += OnChannelUpdatedAsync;
            DiscordSocketClient.GuildAvailable  += OnGuildAvailableAsync;
            DiscordSocketClient.JoinedGuild     += OnJoinedGuildAsync;
            DiscordSocketClient.MessageDeleted  += OnMessageDeletedAsync;
            DiscordSocketClient.MessageReceived += OnMessageReceivedAsync;
            DiscordSocketClient.MessageUpdated  += OnMessageUpdatedAsync;
            DiscordSocketClient.ReactionAdded   += OnReactionAddedAsync;
            DiscordSocketClient.ReactionRemoved += OnReactionRemovedAsync;
            DiscordSocketClient.Ready           += OnReadyAsync;
            DiscordSocketClient.Connected       += OnConnectedAsync;
            DiscordSocketClient.Disconnected    += OnDisconnectedAsync;
            DiscordSocketClient.RoleCreated     += OnRoleCreatedAsync;
            DiscordSocketClient.RoleUpdated     += OnRoleUpdatedAsync;
            DiscordSocketClient.UserBanned      += OnUserBannedAsync;
            DiscordSocketClient.UserJoined      += OnUserJoinedAsync;
            DiscordSocketClient.UserLeft        += OnUserLeftAsync;

            DiscordSocketClient.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;

            return Task.CompletedTask;
        }

        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            DiscordSocketClient.ChannelCreated        -= OnChannelCreatedAsync;
            DiscordSocketClient.ChannelUpdated        -= OnChannelUpdatedAsync;
            DiscordSocketClient.GuildAvailable        -= OnGuildAvailableAsync;
            DiscordSocketClient.GuildMemberUpdated    -= OnGuildMemberUpdatedAsync;
            DiscordSocketClient.JoinedGuild           -= OnJoinedGuildAsync;
            DiscordSocketClient.MessageDeleted        -= OnMessageDeletedAsync;
            DiscordSocketClient.MessageReceived       -= OnMessageReceivedAsync;
            DiscordSocketClient.MessageUpdated        -= OnMessageUpdatedAsync;
            DiscordSocketClient.ReactionAdded         -= OnReactionAddedAsync;
            DiscordSocketClient.ReactionRemoved       -= OnReactionRemovedAsync;
            DiscordSocketClient.Ready                 -= OnReadyAsync;
            DiscordSocketClient.UserBanned            -= OnUserBannedAsync;
            DiscordSocketClient.UserJoined            -= OnUserJoinedAsync;
            DiscordSocketClient.UserLeft              -= OnUserLeftAsync;
            DiscordSocketClient.UserVoiceStateUpdated -= OnUserVoiceStateUpdatedAsync;

            return Task.CompletedTask;
        }

        private Task OnChannelCreatedAsync(SocketChannel channel)
        {
            MessageDispatcher.Publish(new ChannelCreatedNotification(channel), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnChannelUpdatedAsync(SocketChannel oldChannel, SocketChannel newChannel)
        {
            MessageDispatcher.Publish(new ChannelUpdatedNotification(oldChannel, newChannel), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnConnectedAsync()
        {
            MessageDispatcher.Publish(ConnectedNotification.Default, _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnDisconnectedAsync(Exception arg)
        {
            MessageDispatcher.Publish(new DisconnectedNotification(arg), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnGuildAvailableAsync(SocketGuild guild)
        {
            MessageDispatcher.Publish(new GuildAvailableNotification(guild), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnGuildMemberUpdatedAsync(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            MessageDispatcher.Publish(new GuildMemberUpdatedNotification(oldMember, newMember), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnJoinedGuildAsync(SocketGuild guild)
        {
            MessageDispatcher.Publish(new JoinedGuildNotification(guild), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnMessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            MessageDispatcher.Publish(new MessageDeletedNotification(message, channel), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnMessageReceivedAsync(SocketMessage message)
        {
            MessageDispatcher.Publish(new MessageReceivedNotification(message), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnMessageUpdatedAsync(
            Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage,
            ISocketMessageChannel channel)
        {
            MessageDispatcher.Publish(new MessageUpdatedNotification(oldMessage, newMessage, channel),
                _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnReactionAddedAsync(
            Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            MessageDispatcher.Publish(new ReactionAddedNotification(message, channel, reaction), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnReactionRemovedAsync(
            Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            MessageDispatcher.Publish(new ReactionRemovedNotification(message, channel, reaction), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnReadyAsync()
        {
            MessageDispatcher.Publish(ReadyNotification.Default, _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnRoleCreatedAsync(SocketRole role)
        {
            MessageDispatcher.Publish(new RoleCreatedNotification(role), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnRoleUpdatedAsync(SocketRole oldRole, SocketRole newRole)
        {
            MessageDispatcher.Publish(new RoleUpdatedNotification(oldRole, newRole), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnUserBannedAsync(SocketUser user, SocketGuild guild)
        {
            MessageDispatcher.Publish(new UserBannedNotification(user, guild), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnUserJoinedAsync(SocketGuildUser guildUser)
        {
            MessageDispatcher.Publish(new UserJoinedNotification(guildUser), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnUserLeftAsync(SocketGuildUser guildUser)
        {
            MessageDispatcher.Publish(new UserLeftNotification(guildUser), _cancellationToken);

            return Task.CompletedTask;
        }

        private Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState old, SocketVoiceState @new)
        {
            MessageDispatcher.Publish(new UserVoiceStateNotification(user, old, @new), _cancellationToken);

            return Task.CompletedTask;
        }
    }
}