using System.Threading;
using System.Threading.Tasks;
using HuTao.Data;
using HuTao.Services.Core.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.TimeTracking;

public class GenshinTimeTrackingBehavior : INotificationHandler<ReadyNotification>
{
    private readonly GenshinTimeTrackingService _tracking;
    private readonly HuTaoContext _db;

    public GenshinTimeTrackingBehavior(GenshinTimeTrackingService tracking, HuTaoContext db)
    {
        _tracking = tracking;
        _db       = db;
    }

    public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        var guilds = await _db.Guilds.ToListAsync(cancellationToken);
        foreach (var guild in guilds)
        {
            await _tracking.TrackGenshinTime(guild);
        }
    }
}