using System.Threading;
using System.Threading.Tasks;
using HuTao.Data;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class UserTrackingBehavior :
    INotificationHandler<GuildMemberUpdatedNotification>,
    INotificationHandler<UserJoinedNotification>
{
    private readonly HuTaoContext _db;

    public UserTrackingBehavior(HuTaoContext db) { _db = db; }

    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var user = notification.NewMember;
        if (user.Username is null) return;

        await _db.Users.TrackUserAsync(user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(UserJoinedNotification notification, CancellationToken cancellationToken)
    {
        await _db.Users.TrackUserAsync(notification.GuildUser, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}