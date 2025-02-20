using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Services.Core;
using HuTao.Services.Core.Messages;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class GuildConfigureBehavior(AuthorizationService auth)
    : INotificationHandler<GuildAvailableNotification>,
      INotificationHandler<JoinedGuildNotification>
{
    public Task Handle(GuildAvailableNotification notification, CancellationToken cancellationToken)
        => ConfigureGuildAsync(notification.Guild, cancellationToken);

    public Task Handle(JoinedGuildNotification notification, CancellationToken cancellationToken)
        => ConfigureGuildAsync(notification.Guild, cancellationToken);

    private Task ConfigureGuildAsync(IGuild guild, CancellationToken cancellationToken)
        => auth.AutoConfigureGuild(guild, cancellationToken);
}