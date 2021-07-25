using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Zhongli.Data;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.TimeTracking
{
    public class GenshinTimeTrackingBehavior : INotificationHandler<ReadyNotification>
    {
        private readonly ZhongliContext _db;
        private readonly GenshinTimeTrackingService _tracking;

        public GenshinTimeTrackingBehavior(ZhongliContext db, GenshinTimeTrackingService tracking)
        {
            _db       = db;
            _tracking = tracking;
        }

        public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            var guilds = await _db.Guilds.ToAsyncEnumerable().ToListAsync(cancellationToken);
            foreach (var rules in guilds.Select(guild => guild.GenshinRules).Where(rules => rules is not null))
            {
                _tracking.TrackGenshinTime(rules!);
            }
        }
    }
}