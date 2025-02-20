using System.Threading;
using System.Threading.Tasks;
using HuTao.Data;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;

namespace HuTao.Bot.Behaviors;

public class UserTrackingBehavior(HuTaoContext db)
    : INotificationHandler<GuildMemberUpdatedNotification>,
      INotificationHandler<UserJoinedNotification>
{
    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var user = notification.NewMember;
        if (user.Username is null) return;

        await db.Users.TrackUserAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(UserJoinedNotification notification, CancellationToken cancellationToken)
    {
        await db.Users.TrackUserAsync(notification.GuildUser, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}