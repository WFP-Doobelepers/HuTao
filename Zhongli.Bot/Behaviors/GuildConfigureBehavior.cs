using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Zhongli.Data;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors;

public class GuildConfigureBehavior :
    INotificationHandler<GuildAvailableNotification>,
    INotificationHandler<JoinedGuildNotification>
{
    private readonly AuthorizationService _auth;
    private readonly ZhongliContext _db;

    public GuildConfigureBehavior(AuthorizationService auth, ZhongliContext db)
    {
        _auth = auth;
        _db   = db;
    }

    public Task Handle(GuildAvailableNotification notification, CancellationToken cancellationToken)
        => ConfigureGuildAsync(notification.Guild, cancellationToken);

    public Task Handle(JoinedGuildNotification notification, CancellationToken cancellationToken)
        => ConfigureGuildAsync(notification.Guild, cancellationToken);

    private async Task ConfigureGuildAsync(IGuild guild, CancellationToken cancellationToken)
    {
        await _db.Guilds.TrackGuildAsync(guild, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _auth.AutoConfigureGuild(guild, cancellationToken);
    }
}