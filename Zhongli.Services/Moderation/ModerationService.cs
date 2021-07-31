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
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
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

        public async Task UnmuteAsync(Mute mute, CancellationToken cancellationToken)
        {
            var guildEntity = await _db.Guilds.FindByIdAsync(mute.GuildId, cancellationToken);
            var muteRoleId = guildEntity?.MuteRoleId;
            if (muteRoleId is null)
                return;

            var guild = _client.GetGuild(mute.GuildId);
            var user = guild.GetUser(mute.UserId);

            await user.RemoveRoleAsync(muteRoleId.Value);

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
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(mute.TimeLeft!.Value, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                        await UnmuteAsync(mute, cancellationToken);
                }, cancellationToken);
            }

            _db.Add(mute);
            await _db.SaveChangesAsync(cancellationToken);

            await PublishReprimandAsync(details, mute, cancellationToken);
            return mute;
        }

        public async Task<WarningResult> WarnAsync(uint amount, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var warning = new Warning(amount, details);

            _db.Add(warning);
            await _db.SaveChangesAsync(cancellationToken);

            var request = new ReprimandRequest<Warning, WarningResult>(details, warning);
            return await PublishReprimandAsync(details, request, cancellationToken);
        }

        private async Task<T> PublishReprimandAsync<T>(ReprimandDetails details, IRequest<T> request,
            CancellationToken cancellationToken) where T : ReprimandResult
        {
            var result = await _mediator.Send(request, cancellationToken);
            await PublishReprimandAsync(details, result, cancellationToken);

            return result;
        }

        private async Task PublishReprimandAsync(ReprimandDetails details, ReprimandResult result,
            CancellationToken cancellationToken)
        {
            await _mediator.Publish(new ReprimandNotification(details, result), cancellationToken);
        }

        public async Task<NoticeResult> NoticeAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var notice = new Notice(details);

            _db.Add(notice);
            await _db.SaveChangesAsync(cancellationToken);

            var request = new ReprimandRequest<Notice, NoticeResult>(details, notice);
            return await PublishReprimandAsync(details, request, cancellationToken);
        }

        public async Task NoteAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var note = _db.Add(new Note(details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            await PublishReprimandAsync(details, note, cancellationToken);
        }

        public async Task<Kick?> TryKickAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = details.User;
                await user.KickAsync(details.Reason);

                var kick = _db.Add(new Kick(details)).Entity;
                await _db.SaveChangesAsync(cancellationToken);

                await PublishReprimandAsync(details, kick, cancellationToken);
                return kick;
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.Forbidden)
                    return null;

                throw;
            }
        }

        public async Task<Ban?> TryBanAsync(uint? deleteDays, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = details.User;
                await user.BanAsync((int) (deleteDays ?? 1), details.Reason);

                var ban = _db.Add(new Ban(deleteDays ?? 1, details)).Entity;
                await _db.SaveChangesAsync(cancellationToken);

                await PublishReprimandAsync(details, ban, cancellationToken);
                return ban;
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.Forbidden)
                    return null;

                throw;
            }
        }

        public Task HideReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details)
            => UpdateReprimandAsync(reprimand, details, ReprimandStatus.Hidden);

        public async Task DeleteReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details)
        {
            _db.Remove(reprimand.Action);
            if (reprimand.ModifiedAction is not null)
                _db.Remove(reprimand.ModifiedAction);

            _db.Remove(reprimand);
            await _db.SaveChangesAsync();

            ModifyReprimandAsync(details, reprimand, ReprimandStatus.Deleted);
            await _mediator.Publish(new ModifiedReprimandNotification(details, reprimand));
        }

        public Task UpdateReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details)
            => UpdateReprimandAsync(reprimand, details, ReprimandStatus.Updated);

        private static ReprimandAction ModifyReprimandAsync(ModifiedReprimand details,
            ReprimandAction reprimand,
            ReprimandStatus status)
        {
            reprimand.Status         = status;
            reprimand.ModifiedAction = new ModerationAction(details);

            return reprimand;
        }

        private async Task UpdateReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details,
            ReprimandStatus status)
        {
            _db.Update(ModifyReprimandAsync(details, reprimand, status));
            await _db.SaveChangesAsync();

            await _mediator.Publish(new ModifiedReprimandNotification(details, reprimand));
        }
    }
}