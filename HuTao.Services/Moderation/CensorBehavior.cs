using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.Core;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Moderation;

public class CensorBehavior :
    INotificationHandler<MessageReceivedNotification>,
    INotificationHandler<MessageUpdatedNotification>,
    INotificationHandler<UserJoinedNotification>,
    INotificationHandler<GuildMemberUpdatedNotification>
{
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;
    private readonly ModerationService _moderation;

    public CensorBehavior(HuTaoContext db, IMemoryCache cache, ModerationService moderation)
    {
        _db         = db;
        _cache      = cache;
        _moderation = moderation;
    }

    public Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
        => ProcessUser(notification.NewMember, cancellationToken);

    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        => ProcessMessage(notification.Message, cancellationToken);

    public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
        => ProcessMessage(notification.NewMessage, cancellationToken);

    public Task Handle(UserJoinedNotification notification, CancellationToken cancellationToken)
        => ProcessUser(notification.GuildUser, cancellationToken);

    private async Task ProcessMessage(SocketMessage message, CancellationToken cancellationToken = default)
    {
        if (message is not IUserMessage
            {
                Author: IGuildUser user,
                Author.IsBot: false,
                Channel: ITextChannel channel
            }) return;

        var rules = await _db.Guilds.GetRulesAsync(channel.Guild, _cache, cancellationToken);
        if (rules is null || rules.CensorExclusions.Any(e => e.Judge(channel, user)))
            return;

        await _db.Users.TrackUserAsync(user, cancellationToken);
        var currentUser = await channel.Guild.GetCurrentUserAsync();

        var censors = rules.Triggers.OfType<Censor>().Where(c => c.IsActive).ToList()
            .Where(c => c.Exclusions.All(e => !e.Judge(channel, user)))
            .Where(c => c.Category?.CensorExclusions.All(e => !e.Judge(channel, user)) ?? true);

        foreach (var censor in censors.Where(c => c.Regex().IsMatch(message.Content)))
        {
            var details = new ReprimandDetails(
                user, currentUser, "[Censor Triggered]",
                censor, Category: censor.Category);

            await _moderation.CensorAsync(message, Length(censor), details, cancellationToken);
        }

        await ProcessUser(user, cancellationToken);

        TimeSpan? Length(ITrigger censor) => censor.Category?.CensoredExpiryLength ?? rules.CensoredExpiryLength;
    }

    private async Task ProcessUser(IGuildUser user, CancellationToken cancellationToken)
    {
        if (user.IsBot) return;

        var rules = await _db.Guilds.GetRulesAsync(user.Guild, _cache, cancellationToken);
        if (rules is null || rules.CensorExclusions.Any(e => e.Judge(null, user)))
            return;

        await _db.Users.TrackUserAsync(user, cancellationToken);
        var currentUser = await user.Guild.GetCurrentUserAsync();

        if (string.IsNullOrWhiteSpace(rules.NameReplacement)) return;
        var hasNickname = !string.IsNullOrWhiteSpace(user.Nickname);

        var censors = rules.Triggers.OfType<Censor>().Where(c => c.IsActive).ToList()
            .Where(c => c.Exclusions.All(e => !e.Judge(null, user)))
            .Where(c => c.Category?.CensorExclusions.All(e => !e.Judge(null, user)) ?? true)
            .ToList();

        foreach (var censor in censors.Where(c
            => rules.CensorNicknames && hasNickname
            && c.Regex().IsMatch(user.Nickname)))
        {
            var details = new ReprimandDetails(
                user, currentUser, "[Nickname Censor Triggered]",
                censor, Category: censor.Category);

            var result = await _moderation.CensorNameAsync(
                user.Nickname, rules.NameReplacement,
                Length(censor), details, cancellationToken);

            if (result is not null) return;
        }

        foreach (var censor in censors.Where(c
            => rules.CensorUsernames && !hasNickname
            && c.Regex().IsMatch(user.Username)))
        {
            var details = new ReprimandDetails(
                user, currentUser, "[Username Censor Triggered]",
                censor, Category: censor.Category);

            var result = await _moderation.CensorNameAsync(
                user.Username, rules.NameReplacement,
                Length(censor), details, cancellationToken);

            if (result is not null) return;
        }

        TimeSpan? Length(ITrigger censor) => censor.Category?.CensoredExpiryLength ?? rules.CensoredExpiryLength;
    }
}