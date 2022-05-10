using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;

namespace HuTao.Services.Sticky;

public class StickyBehavior : INotificationHandler<MessageReceivedNotification>
{
    private readonly HuTaoContext _db;
    private readonly StickyService _sticky;

    public StickyBehavior(StickyService sticky, HuTaoContext db)
    {
        _sticky = sticky;
        _db     = db;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Channel is not ITextChannel channel || notification.Message.Author.IsBot)
            return;

        var guild = await _db.Guilds.TrackGuildAsync(channel.Guild, cancellationToken);

        var sticky = guild.StickyMessages.FirstOrDefault(m => m.ChannelId == channel.Id && m.IsActive);
        if (sticky is null) return;

        await _sticky.SendStickyMessage(sticky, channel);
    }
}