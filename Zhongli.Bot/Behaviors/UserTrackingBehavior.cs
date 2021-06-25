using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Zhongli.Data;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Behaviors
{
    public class UserTrackingBehavior :
        INotificationHandler<GuildMemberUpdatedNotification>,
        INotificationHandler<MessageReceivedNotification>,
        INotificationHandler<UserJoinedNotification>
    {
        private readonly ZhongliContext _db;

        public UserTrackingBehavior(ZhongliContext db) { _db = db; }

        public Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken) =>
            _db.Users.TrackUserAsync(notification.NewMember, cancellationToken);

        public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
            => notification.Message.Author is IGuildUser { Guild: { } } author
                ? _db.Users.TrackUserAsync(author, cancellationToken)
                : Task.CompletedTask;

        public Task Handle(UserJoinedNotification notification, CancellationToken cancellationToken) =>
            _db.Users.TrackUserAsync(notification.GuildUser, cancellationToken);
    }
}