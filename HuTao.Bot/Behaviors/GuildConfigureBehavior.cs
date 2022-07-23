using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Services.Core;
using HuTao.Services.Core.Messages;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class GuildConfigureBehavior :
    INotificationHandler<GuildAvailableNotification>,
    INotificationHandler<JoinedGuildNotification>
{
    private readonly AuthorizationService _auth;

    public GuildConfigureBehavior(AuthorizationService auth) { _auth = auth; }

    public Task Handle(GuildAvailableNotification notification, CancellationToken cancellationToken)
        => ConfigureGuildAsync(notification.Guild, cancellationToken);

    public Task Handle(JoinedGuildNotification notification, CancellationToken cancellationToken)
        => ConfigureGuildAsync(notification.Guild, cancellationToken);

    private Task ConfigureGuildAsync(IGuild guild, CancellationToken cancellationToken)
        => _auth.AutoConfigureGuild(guild, cancellationToken);
}