using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Services.Core;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class GuildConfigureBehavior :
    INotificationHandler<GuildAvailableNotification>,
    INotificationHandler<JoinedGuildNotification>
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private readonly AuthorizationService _auth;
    private readonly HuTaoContext _db;

    public GuildConfigureBehavior(AuthorizationService auth, HuTaoContext db)
    {
        _auth = auth;
        _db   = db;
    }

    public Task Handle(GuildAvailableNotification notification, CancellationToken cancellationToken)
        => ConfigureGuildAsync(notification.Guild, cancellationToken);

    public Task Handle(JoinedGuildNotification notification, CancellationToken cancellationToken)
        => ConfigureGuildAsync(notification.Guild, cancellationToken);

    private async Task ConfigureGuildAsync(IGuild? guild, CancellationToken cancellationToken)
    {
        if (guild is null) return;

        try
        {
            await Semaphore.WaitAsync(cancellationToken);

            await _db.Guilds.TrackGuildAsync(guild, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await _auth.AutoConfigureGuild(guild, cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}