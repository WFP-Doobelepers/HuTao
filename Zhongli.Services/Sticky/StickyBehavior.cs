using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Zhongli.Data;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Sticky;

public class StickyBehavior : INotificationHandler<MessageReceivedNotification>
{
    private readonly StickyService _sticky;
    private readonly ZhongliContext _db;

    public StickyBehavior(StickyService sticky, ZhongliContext db)
    {
        _sticky = sticky;
        _db     = db;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Channel is not ITextChannel channel || notification.Message.Author.IsBot)
            return;

        var guild = await _db.Guilds.TrackGuildAsync(channel.Guild, cancellationToken);

        var sticky = guild.StickyMessages.FirstOrDefault(m => m.ChannelId == channel.Id);
        if (sticky is null) return;

        await _sticky.SendStickyMessage(sticky, channel);
    }
}