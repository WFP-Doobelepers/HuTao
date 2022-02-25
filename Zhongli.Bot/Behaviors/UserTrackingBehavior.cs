using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Zhongli.Data;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors;

public class UserTrackingBehavior :
    INotificationHandler<GuildMemberUpdatedNotification>,
    INotificationHandler<MessageReceivedNotification>,
    INotificationHandler<UserJoinedNotification>,
    INotificationHandler<ReadyNotification>
{
    private readonly ZhongliContext _db;
    private bool _ready;

    public UserTrackingBehavior(ZhongliContext db) { _db = db; }

    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        if (!_ready) return;

        var user = notification.NewMember;
        if (user.Username is null) return;

        await _db.Users.TrackUserAsync(user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Author is IGuildUser { Username: { } } user)
        {
            await _db.Users.TrackUserAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        _ready = true;

        return Task.CompletedTask;
    }

    public async Task Handle(UserJoinedNotification notification, CancellationToken cancellationToken)
    {
        await _db.Users.TrackUserAsync(notification.GuildUser, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}