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
using Zhongli.Data.Models.Moderation.Infractions;
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

        public ModerationService(DiscordSocketClient client, ZhongliContext db)
        {
            _client = client;
            _db     = db;
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

        public async Task<bool> TryMuteAsync(IGuildUser user, IGuildUser moderator,
            TimeSpan? length = null, string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var guild = await _db.Guilds.FindByIdAsync(user.GuildId, cancellationToken);
            var muteRole = guild?.MuteRoleId;

            if (muteRole is null || user.HasRole(muteRole.Value))
                return false;

            if (ActiveMutes.TryGetValue(user.Id, out var activeMute))
            {
                activeMute!.EndedAt = DateTimeOffset.UtcNow;
                ActiveMutes.TryRemove(moderator.Id, out _);
            }

            var details = new ReprimandDetails(user, ModerationActionType.Added, reason);
            var mute = new Mute(DateTimeOffset.UtcNow, length, details).WithModerator(moderator);

            await user.AddRoleAsync(muteRole.Value);
            if (mute.TimeLeft is not null)
                _ = EnqueueMuteTimer(user, muteRole.Value, mute.TimeLeft.Value, mute, cancellationToken);

            _db.Add(mute);
            await _db.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<int> WarnAsync(IGuildUser user, IGuildUser moderator, uint warnCount, string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var details = new ReprimandDetails(user, ModerationActionType.Added);
            var warning = new Warning(warnCount, details).WithModerator(moderator);

            var userEntity = await _db.Users.TrackUserAsync(user, cancellationToken);
            var warnings = _db.Set<Warning>()
                .AsQueryable()
                .Where(w => w.GuildId == user.GuildId)
                .Where(w => w.UserId == user.Id)
                .Sum(w => w.Amount);

            userEntity.WarningCount = (int) warnings;
            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(new WarnNotification(user, moderator, warning), cancellationToken);
            return userEntity.WarningCount;
        }

        public async Task<bool> TryKickAsync(IGuildUser user, IGuildUser moderator, string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await user.KickAsync(reason);

                var details = new ReprimandDetails(user, ModerationActionType.Added);
                _db.Add(new Kick(details).WithModerator(moderator));

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

        public async Task<bool> TryBanAsync(IGuildUser user, IGuildUser mod,
            uint deleteDays = 1, string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await user.BanAsync((int) deleteDays, reason);

                var details = new ReprimandDetails(user, ModerationActionType.Added);
                _db.Add(new Ban(deleteDays, details).WithModerator(mod));

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