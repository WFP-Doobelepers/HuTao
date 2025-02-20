using System.Threading;
using System.Threading.Tasks;
using HuTao.Data;
using HuTao.Services.Core.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.TimeTracking;

public class GenshinTimeTrackingBehavior(GenshinTimeTrackingService tracking, HuTaoContext db)
    : INotificationHandler<ReadyNotification>
{
    public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        var guilds = await db.Guilds.ToListAsync(cancellationToken);
        foreach (var guild in guilds)
        {
            await tracking.TrackGenshinTime(guild);
        }
    }
}