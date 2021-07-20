using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation
{
    public class ModerationService
    {
        private readonly DiscordSocketClient _client;
        private readonly ZhongliContext _db;
        private readonly IMediator _mediator;

        public ModerationService(IMediator mediator, DiscordSocketClient client, ZhongliContext db)
        {
            _mediator = mediator;
            _client   = client;
            _db       = db;
        }

        private ConcurrentDictionary<ulong, Mute> ActiveMutes { get; } = new();

        public async Task EnqueueMuteTimer(Mute mute, CancellationToken cancellationToken)
        {
            var guildEntity = await _db.Guilds.FindByIdAsync(mute.GuildId, cancellationToken);
            var muteRoleId = guildEntity?.MuteRoleId;
            if (muteRoleId is null)
                return;

            var guild = _client.GetGuild(mute.GuildId);
            var user = guild.GetUser(mute.UserId);

            await EnqueueMuteTimer(user, muteRoleId.Value, mute.TimeLeft!.Value, mute, cancellationToken);
        }

        private async Task EnqueueMuteTimer(IGuildUser user, ulong roleId, TimeSpan length, Mute mute,
            CancellationToken cancellationToken = default)
        {
            if (!ActiveMutes.TryAdd(mute.UserId, mute))
                return;

            await Task.Delay(length, cancellationToken);
            await user.RemoveRoleAsync(roleId);

            mute.EndedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Mute?> TryMuteAsync(TimeSpan? length, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var guild = await _db.Guilds.FindByIdAsync(details.User.Guild.Id, cancellationToken);
            var muteRole = guild?.MuteRoleId;

            if (muteRole is null || details.User.HasRole(muteRole.Value))
                return null;

            if (ActiveMutes.TryGetValue(details.User.Id, out var activeMute))
            {
                activeMute!.EndedAt = DateTimeOffset.UtcNow;
                ActiveMutes.TryRemove(details.Moderator.Id, out _);
            }

            var mute = new Mute(DateTimeOffset.UtcNow, length, details);

            await details.User.AddRoleAsync(muteRole.Value);
            if (mute.TimeLeft is not null)
                _ = EnqueueMuteTimer(details.User, muteRole.Value, mute.TimeLeft.Value, mute, cancellationToken);

            _db.Add(mute);
            await _db.SaveChangesAsync(cancellationToken);

            return mute;
        }

        public async Task<WarningResult> WarnAsync(uint amount, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var warning = new Warning(amount, details);

            _db.Add(warning);
            await _db.SaveChangesAsync(cancellationToken);

            return await _mediator.Send(
                new ReprimandRequest<Warning, WarningResult>(details.User, details.Moderator, warning),
                cancellationToken);
        }

        public async Task<NoticeResult> NoticeAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var notice = new Notice(details);

            _db.Add(notice);
            await _db.SaveChangesAsync(cancellationToken);

            return await _mediator.Send(
                new ReprimandRequest<Notice, NoticeResult>(details.User, details.Moderator, notice),
                cancellationToken);
        }

        public async Task NoteAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            _db.Add(new Note(details));
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Kick?> TryKickAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await details.User.KickAsync(details.Reason);

                var kick = _db.Add(new Kick(details)).Entity;
                await _db.SaveChangesAsync(cancellationToken);

                return kick;
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.Forbidden)
                    return null;

                throw;
            }
        }

        public async Task<Ban?> TryBanAsync(uint? deleteDays,
            ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await details.User.BanAsync((int) (deleteDays ?? 1), details.Reason);

                var ban = _db.Add(new Ban(deleteDays ?? 1, details)).Entity;
                await _db.SaveChangesAsync(cancellationToken);

                return ban;
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.Forbidden)
                    return null;

                throw;
            }
        }
    }
}