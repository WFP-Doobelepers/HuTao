using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.Core.Listeners;

/// <summary>
///     Listens for events from an <see cref="DiscordSocketClient" /> and dispatches them to the rest of the application,
///     through an <see cref="IMediator" />.
/// </summary>
public class DiscordSocketListener
{
    private CancellationToken _cancellationToken;

    /// <summary>
    ///     Constructs a new <see cref="DiscordSocketListener" /> with the given dependencies.
    /// </summary>
    public DiscordSocketListener(DiscordSocketClient discordSocketClient, IServiceScopeFactory serviceScope)
    {
        DiscordSocketClient = discordSocketClient;
        ServiceScope        = serviceScope;
    }

    /// <summary>
    ///     The <see cref="DiscordSocketClient" /> to be listened to.
    /// </summary>
    private DiscordSocketClient DiscordSocketClient { get; }

    /// <summary>
    ///     Gets a scoped <see cref="IMediator" />.
    /// </summary>
    private IMediator Mediator
    {
        get
        {
            var scope = ServiceScope.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IMediator>();
        }
    }

    /// <summary>
    ///     The <see cref="IServiceScopeFactory" /> to be used.
    /// </summary>
    private IServiceScopeFactory ServiceScope { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken                        =  cancellationToken;
        DiscordSocketClient.ChannelCreated        += OnChannelCreatedAsync;
        DiscordSocketClient.ChannelUpdated        += OnChannelUpdatedAsync;
        DiscordSocketClient.GuildAvailable        += OnGuildAvailableAsync;
        DiscordSocketClient.GuildMemberUpdated    += OnGuildMemberUpdatedAsync;
        DiscordSocketClient.InteractionCreated    += OnInteractionCreatedAsync;
        DiscordSocketClient.JoinedGuild           += OnJoinedGuildAsync;
        DiscordSocketClient.MessageDeleted        += OnMessageDeletedAsync;
        DiscordSocketClient.MessagesBulkDeleted   += OnMessagesBulkDeletedAsync;
        DiscordSocketClient.MessageReceived       += OnMessageReceivedAsync;
        DiscordSocketClient.MessageUpdated        += OnMessageUpdatedAsync;
        DiscordSocketClient.ReactionAdded         += OnReactionAddedAsync;
        DiscordSocketClient.ReactionRemoved       += OnReactionRemovedAsync;
        DiscordSocketClient.Ready                 += OnReadyAsync;
        DiscordSocketClient.Connected             += OnConnectedAsync;
        DiscordSocketClient.Disconnected          += OnDisconnectedAsync;
        DiscordSocketClient.RoleCreated           += OnRoleCreatedAsync;
        DiscordSocketClient.RoleUpdated           += OnRoleUpdatedAsync;
        DiscordSocketClient.UserBanned            += OnUserBannedAsync;
        DiscordSocketClient.UserJoined            += OnUserJoinedAsync;
        DiscordSocketClient.UserLeft              += OnUserLeftAsync;
        DiscordSocketClient.UserUnbanned          += OnUserUnbannedAsync;
        DiscordSocketClient.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        DiscordSocketClient.ChannelCreated        -= OnChannelCreatedAsync;
        DiscordSocketClient.ChannelUpdated        -= OnChannelUpdatedAsync;
        DiscordSocketClient.GuildAvailable        -= OnGuildAvailableAsync;
        DiscordSocketClient.GuildMemberUpdated    -= OnGuildMemberUpdatedAsync;
        DiscordSocketClient.InteractionCreated    -= OnInteractionCreatedAsync;
        DiscordSocketClient.JoinedGuild           -= OnJoinedGuildAsync;
        DiscordSocketClient.MessageDeleted        -= OnMessageDeletedAsync;
        DiscordSocketClient.MessagesBulkDeleted   -= OnMessagesBulkDeletedAsync;
        DiscordSocketClient.MessageReceived       -= OnMessageReceivedAsync;
        DiscordSocketClient.MessageUpdated        -= OnMessageUpdatedAsync;
        DiscordSocketClient.ReactionAdded         -= OnReactionAddedAsync;
        DiscordSocketClient.ReactionRemoved       -= OnReactionRemovedAsync;
        DiscordSocketClient.Ready                 -= OnReadyAsync;
        DiscordSocketClient.Connected             -= OnConnectedAsync;
        DiscordSocketClient.Disconnected          -= OnDisconnectedAsync;
        DiscordSocketClient.RoleCreated           -= OnRoleCreatedAsync;
        DiscordSocketClient.RoleUpdated           -= OnRoleUpdatedAsync;
        DiscordSocketClient.UserBanned            -= OnUserBannedAsync;
        DiscordSocketClient.UserJoined            -= OnUserJoinedAsync;
        DiscordSocketClient.UserLeft              -= OnUserLeftAsync;
        DiscordSocketClient.UserUnbanned          -= OnUserUnbannedAsync;
        DiscordSocketClient.UserVoiceStateUpdated -= OnUserVoiceStateUpdatedAsync;

        return Task.CompletedTask;
    }

    private Task OnChannelCreatedAsync(SocketChannel channel)
        => Mediator.Publish(new ChannelCreatedNotification(channel), _cancellationToken);

    private Task OnChannelUpdatedAsync(SocketChannel oldChannel, SocketChannel newChannel)
        => Mediator.Publish(new ChannelUpdatedNotification(oldChannel, newChannel), _cancellationToken);

    private Task OnConnectedAsync()
        => Mediator.Publish(ConnectedNotification.Default, _cancellationToken);

    private Task OnDisconnectedAsync(Exception arg)
        => Mediator.Publish(new DisconnectedNotification(arg), _cancellationToken);

    private Task OnGuildAvailableAsync(SocketGuild guild)
        => Mediator.Publish(new GuildAvailableNotification(guild), _cancellationToken);

    private Task OnGuildMemberUpdatedAsync(
        Cacheable<SocketGuildUser, ulong> oldMember, SocketGuildUser newMember)
        => Mediator.Publish(new GuildMemberUpdatedNotification(oldMember, newMember), _cancellationToken);

    private Task OnInteractionCreatedAsync(SocketInteraction interaction)
        => Mediator.Publish(new InteractionCreatedNotification(interaction), _cancellationToken);

    private Task OnJoinedGuildAsync(SocketGuild guild)
        => Mediator.Publish(new JoinedGuildNotification(guild), _cancellationToken);

    private Task OnMessageDeletedAsync(
        Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
        => Mediator.Publish(new MessageDeletedNotification(message, channel), _cancellationToken);

    private Task OnMessageReceivedAsync(SocketMessage message)
        => Mediator.Publish(new MessageReceivedNotification(message), _cancellationToken);

    private Task OnMessagesBulkDeletedAsync(
        IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, Cacheable<IMessageChannel, ulong> channel)
        => Mediator.Publish(new MessagesBulkDeletedNotification(messages, channel), _cancellationToken);

    private Task OnMessageUpdatedAsync(
        Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        => Mediator.Publish(new MessageUpdatedNotification(oldMessage, newMessage, channel), _cancellationToken);

    private Task OnReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        => Mediator.Publish(new ReactionAddedNotification(message, channel, reaction), _cancellationToken);

    private Task OnReactionRemovedAsync(
        Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        => Mediator.Publish(new ReactionRemovedNotification(message, channel, reaction), _cancellationToken);

    private Task OnReadyAsync()
        => Mediator.Publish(ReadyNotification.Default, _cancellationToken);

    private Task OnRoleCreatedAsync(SocketRole role)
        => Mediator.Publish(new RoleCreatedNotification(role), _cancellationToken);

    private Task OnRoleUpdatedAsync(SocketRole oldRole, SocketRole newRole)
        => Mediator.Publish(new RoleUpdatedNotification(oldRole, newRole), _cancellationToken);

    private Task OnUserBannedAsync(SocketUser user, SocketGuild guild)
        => Mediator.Publish(new UserBannedNotification(user, guild), _cancellationToken);

    private Task OnUserJoinedAsync(SocketGuildUser guildUser)
        => Mediator.Publish(new UserJoinedNotification(guildUser), _cancellationToken);

    private Task OnUserLeftAsync(SocketGuild guild, SocketUser user)
        => Mediator.Publish(new UserLeftNotification(guild, user), _cancellationToken);

    private Task OnUserUnbannedAsync(SocketUser user, SocketGuild guild)
        => Mediator.Publish(new UserUnbannedNotification(user, guild), _cancellationToken);

    private Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState old, SocketVoiceState @new)
        => Mediator.Publish(new UserVoiceStateNotification(user, old, @new), _cancellationToken);
}