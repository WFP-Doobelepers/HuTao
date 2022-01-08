using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Logging;
using Zhongli.Services.Quote;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Sticky;

public class StickyBehavior : INotificationHandler<MessageReceivedNotification>
{
    private readonly ZhongliContext _db;

    public StickyBehavior(ZhongliContext db) { _db = db; }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Channel is not IGuildChannel channel)
            return;

        var guild = await _db.Guilds.TrackGuildAsync(channel.Guild, cancellationToken);
        var messages = guild.StickyMessages.Where(m => m.ChannelId == channel.Id);

        foreach (var sticky in messages)
        {
        }
    }

    private async Task SendStickyAsync(IMessageChannel channel, StickyMessage sticky)
    {
        var embed = new EmbedBuilder();
        await channel.SendMessageAsync(sticky.Content);
    }
}