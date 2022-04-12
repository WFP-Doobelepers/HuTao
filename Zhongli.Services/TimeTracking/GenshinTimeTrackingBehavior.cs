using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.TimeTracking;

public class GenshinTimeTrackingBehavior : INotificationHandler<ReadyNotification>
{
    private readonly GenshinTimeTrackingService _tracking;
    private readonly ZhongliContext _db;

    public GenshinTimeTrackingBehavior(GenshinTimeTrackingService tracking, ZhongliContext db)
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