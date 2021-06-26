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

        public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
        {
            await _db.Users.TrackUserAsync(notification.NewMember, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Message.Author is IGuildUser { Guild: { } } author)
            {
                await _db.Users.TrackUserAsync(author, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task Handle(UserJoinedNotification notification, CancellationToken cancellationToken)
        {
            await _db.Users.TrackUserAsync(notification.GuildUser, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}