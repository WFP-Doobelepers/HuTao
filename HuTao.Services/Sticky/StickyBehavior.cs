using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;

namespace HuTao.Services.Sticky;

public class StickyBehavior(StickyService sticky, HuTaoContext db) : INotificationHandler<MessageReceivedNotification>
{
    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Channel is not ITextChannel channel || notification.Message.Author.IsBot)
            return;

        var guild = await db.Guilds.TrackGuildAsync(channel.Guild, cancellationToken);

        var sticky1 = guild.StickyMessages.FirstOrDefault(m => m.ChannelId == channel.Id && m.IsActive);
        if (sticky1 is null) return;

        await sticky.SendStickyMessage(sticky1, channel);
    }
}