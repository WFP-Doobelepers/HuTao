using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;

namespace HuTao.Services.Moderation;

public class CensorBehavior :
    INotificationHandler<MessageReceivedNotification>,
    INotificationHandler<MessageUpdatedNotification>
{
    private readonly HuTaoContext _db;
    private readonly ModerationService _moderation;

    public CensorBehavior(ModerationService moderation, HuTaoContext db)
    {
        _moderation = moderation;
        _db         = db;
    }

    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        => ProcessMessage(notification.Message, cancellationToken);

    public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
        => ProcessMessage(notification.NewMessage, cancellationToken);

    private async Task ProcessMessage(SocketMessage message, CancellationToken cancellationToken = default)
    {
        var author = message.Author;
        if (author.IsBot || author.IsWebhook
            || author is not IGuildUser user
            || message.Channel is not ITextChannel channel)
            return;

        var guild = channel.Guild;
        var guildEntity = await _db.Guilds.TrackGuildAsync(guild, cancellationToken);
        if (cancellationToken.IsCancellationRequested || guildEntity.ModerationRules is null)
            return;

        if (guildEntity.ModerationRules.CensorExclusions.Any(e => e.Judge(channel, user)))
            return;

        await _db.Users.TrackUserAsync(user, cancellationToken);
        var currentUser = await guild.GetCurrentUserAsync();

        foreach (var censor in guildEntity.ModerationRules.Triggers.OfType<Censor>()
            .Where(c => c.IsActive)
            .Where(c => c.Exclusions.All(e => !e.Judge(channel, user)))
            .Where(c => c.Category?.CensorExclusions.All(e => !e.Judge(channel, user)) ?? true)
            .Where(c => c.Regex().IsMatch(message.Content)))
        {
            var details = new ReprimandDetails(user, currentUser, "[Censor Triggered]", censor, null, censor.Category);
            var length = censor.Category?.CensorTimeRange ?? guildEntity.ModerationRules.CensorTimeRange;

            await _moderation.CensorAsync(message, length, details, cancellationToken);
        }
    }
}