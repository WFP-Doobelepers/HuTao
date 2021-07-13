using System;
using System.Collections.Concurrent;
using System.Linq;
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

namespace Zhongli.Services.Core
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

        public async Task<bool> TryMuteAsync(TimeSpan? length, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var guild = await _db.Guilds.FindByIdAsync(details.User.Guild.Id, cancellationToken);
            var muteRole = guild?.MuteRoleId;

            if (muteRole is null || details.User.HasRole(muteRole.Value))
                return false;

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

            return true;
        }

        public async Task<int> WarnAsync(uint amount, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var warning = new Warning(amount, details);

            var userEntity = await _db.Users.TrackUserAsync(details.User, cancellationToken);
            var warnings = _db.Set<Warning>()
                .AsQueryable()
                .Where(w => w.GuildId == details.User.Guild.Id)
                .Where(w => w.UserId == details.User.Id)
                .Sum(w => w.Amount);

            userEntity.WarningCount = (int) warnings;

            _db.WarningHistory.Add(warning);
            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(new WarnNotification(details.User, details.Moderator, warning), cancellationToken);
            return userEntity.WarningCount;
        }

        public async Task<bool> TryKickAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await details.User.KickAsync(details.Reason);

                _db.Add(new Kick(details));
                await _db.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.Forbidden)
                    return false;

                throw;
            }
        }

        public async Task<bool> TryBanAsync(uint? deleteDays,
            ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await details.User.BanAsync((int) (deleteDays ?? 1), details.Reason);

                _db.Add(new Ban(deleteDays ?? 1, details));
                await _db.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.Forbidden)
                    return false;

                throw;
            }
        }
    }
}