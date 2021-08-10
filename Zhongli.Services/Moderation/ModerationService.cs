using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Utilities;
using IBan = Zhongli.Data.Models.Moderation.Infractions.IBan;

namespace Zhongli.Services.Moderation
{
    public class ModerationService
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceScopeFactory _scope;
        private readonly ModerationLoggingService _logging;
        private readonly ZhongliContext _db;

        public ModerationService(DiscordSocketClient client, ZhongliContext db,
            ModerationLoggingService logging,
            IServiceScopeFactory scope)
        {
            _client = client;
            _db     = db;

            _logging = logging;
            _scope   = scope;
        }

        public async Task CensorAsync(Censored censored, SocketMessage message, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            await message.DeleteAsync();

            var user = await censored.GetUserAsync(_db, cancellationToken);
            var count = user.Reprimands<Censored>()
                .Where(t => t.Censor.Id == censored.Censor.Id);

            if (censored.Censor.IsTriggered((uint) count.LongCount()))
                await TryReprimandTriggerAsync(censored.Censor, details, cancellationToken);

            var request = new ReprimandRequest<Censored>(details, censored);
            await PublishReprimandAsync(request, details, cancellationToken);
        }

        public static async Task ConfigureMuteRoleAsync(IGuild guild, IRole? role)
        {
            role ??= guild.Roles.FirstOrDefault(r => r.Name == "Muted");
            role ??= await guild.CreateRoleAsync("Muted", isMentionable: false);

            var permissions = new OverwritePermissions(
                addReactions: PermValue.Deny,
                sendMessages: PermValue.Deny,
                speak: PermValue.Deny,
                stream: PermValue.Deny);

            foreach (var channel in await guild.GetChannelsAsync())
            {
                await channel.AddPermissionOverwriteAsync(role, permissions);
            }
        }

        public async Task DeleteReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details,
            CancellationToken cancellationToken = default)
        {
            _db.Remove(reprimand.Action);
            if (reprimand.ModifiedAction is not null)
                _db.Remove(reprimand.ModifiedAction);

            _db.Remove(reprimand);
            await _db.SaveChangesAsync(cancellationToken);

            await UpdateReprimandAsync(reprimand, details, ReprimandStatus.Expired, cancellationToken);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public async Task ExpireReprimandAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var reprimand = await _db.Set<ReprimandAction>().AsAsyncEnumerable().OfType<IExpirable>()
                .FirstAsync(e => e.Id == id, cancellationToken);

            await (reprimand switch
            {
                Ban ban         => ExpireBanAsync(ban, cancellationToken),
                Mute mute       => ExpireMuteAsync(mute, cancellationToken),
                Notice notice   => ExpireReprimandAsync(notice, cancellationToken),
                Warning warning => ExpireReprimandAsync(warning, cancellationToken)
            });
        }

        public Task HideReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details,
            CancellationToken cancellationToken = default)
            => UpdateReprimandAsync(reprimand, details, ReprimandStatus.Hidden, cancellationToken);

        public async Task NoteAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var note = _db.Add(new Note(details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            await _logging.PublishReprimandAsync(note, details, cancellationToken);
        }

        public Task UpdateReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details,
            CancellationToken cancellationToken = default)
            => UpdateReprimandAsync(reprimand, details, ReprimandStatus.Updated, cancellationToken);

        public async Task<ReprimandResult?> TryBanAsync(uint? deleteDays, TimeSpan? length, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = details.User;
                var days = deleteDays ?? 1;

                var ban = _db.Add(new Ban(days, length, details)).Entity;
                await _db.SaveChangesAsync(cancellationToken);
                await _logging.PublishReprimandAsync(ban, details, cancellationToken);

                await user.BanAsync((int) days, details.Reason);
                EnqueueExpirableReprimand(ban, cancellationToken);

                return ban;
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.Forbidden)
                    return null;

                throw;
            }
        }

        public async Task<ReprimandResult?> TryKickAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = details.User;

                var kick = _db.Add(new Kick(details)).Entity;
                await _db.SaveChangesAsync(cancellationToken);
                await _logging.PublishReprimandAsync(kick, details, cancellationToken);

                await user.KickAsync(details.Reason);

                return kick;
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.Forbidden)
                    return null;

                throw;
            }
        }

        public async Task<ReprimandResult?> TryMuteAsync(TimeSpan? length, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var activeMute = await _db.MuteHistory
                .FirstOrDefaultAsync(m => m.IsActive()
                        && m.UserId == details.User.Id
                        && m.GuildId == details.User.Guild.Id,
                    cancellationToken);

            var user = details.User;
            var guildEntity = await _db.Guilds.FindByIdAsync(user.Guild.Id, cancellationToken);

            var muteRole = guildEntity?.ModerationRules.MuteRoleId;
            if (muteRole is null)
                return null;

            await user.AddRoleAsync(muteRole.Value);
            if (user.VoiceChannel is not null)
                await user.ModifyAsync(u => u.Mute = true);

            if (activeMute is not null)
                return null;

            var mute = _db.Add(new Mute(length, details)).Entity;
            await _db.SaveChangesAsync(cancellationToken);

            EnqueueExpirableReprimand(mute, cancellationToken);

            await _logging.PublishReprimandAsync(mute, details, cancellationToken);
            return mute;
        }

        public async Task<ReprimandResult?> TryReprimandTriggerAsync(ITrigger? trigger, ReprimandDetails details,
            CancellationToken cancellationToken) => trigger switch
        {
            IBan b     => await TryBanAsync(b.DeleteDays, b.Length, details, cancellationToken),
            IKick      => await TryKickAsync(details, cancellationToken),
            IMute m    => await TryMuteAsync(m.Length, details, cancellationToken),
            IWarning w => await WarnAsync(w.Count, details, cancellationToken),
            INotice    => await NoticeAsync(details, cancellationToken),
            _          => null
        };

        public async Task<ReprimandResult> NoticeAsync(ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var guild = await details.GetGuildAsync(_db, cancellationToken);
            var notice = new Notice(guild.ModerationRules.NoticeAutoPardonLength, details);

            _db.Add(notice);
            await _db.SaveChangesAsync(cancellationToken);

            EnqueueExpirableReprimand(notice, cancellationToken);

            var request = new ReprimandRequest<Notice>(details, notice);
            return await PublishReprimandAsync(request, details, cancellationToken);
        }

        public async Task<ReprimandResult> WarnAsync(uint amount, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var guild = await details.GetGuildAsync(_db, cancellationToken);
            var warning = new Warning(amount, guild.ModerationRules.WarningAutoPardonLength, details);

            _db.Add(warning);
            await _db.SaveChangesAsync(cancellationToken);

            EnqueueExpirableReprimand(warning, cancellationToken);

            var request = new ReprimandRequest<Warning>(details, warning);
            return await PublishReprimandAsync(request, details, cancellationToken);
        }

        public void EnqueueExpirableReprimand(IExpirable expire, CancellationToken cancellationToken = default)
        {
            if (expire.ExpireAt is not null)
            {
                BackgroundJob.Schedule(()
                        => ExpireReprimandAsync(expire.Id, cancellationToken),
                    expire.ExpireAt.Value);
            }
        }

        private static ReprimandAction ModifyReprimand(ReprimandAction reprimand, ModifiedReprimand details,
            ReprimandStatus status)
        {
            reprimand.Status         = status;
            reprimand.ModifiedAction = new ModerationAction(details);

            return reprimand;
        }

        private async Task ExpireBanAsync(Ban ban, CancellationToken cancellationToken)
        {
            var guild = _client.GetGuild(ban.GuildId);
            var user = guild.GetUser(ban.UserId);

            await guild.RemoveBanAsync(user);
            if (user.VoiceChannel is not null)
                await user.ModifyAsync(u => u.Mute = false);

            await ExpireReprimandAsync(ban, cancellationToken);
        }

        private async Task ExpireMuteAsync(Mute mute, CancellationToken cancellationToken)
        {
            var guildEntity = await _db.Guilds.FindByIdAsync(mute.GuildId, cancellationToken);
            var user = _client.GetGuild(mute.GuildId).GetUser(mute.UserId);

            if (guildEntity?.ModerationRules.MuteRoleId is not null)
                await user.RemoveRoleAsync(guildEntity.ModerationRules.MuteRoleId.Value);

            if (user.VoiceChannel is not null)
                await user.ModifyAsync(u => u.Mute = false);

            await ExpireReprimandAsync(mute, cancellationToken);
        }

        private async Task ExpireReprimandAsync<T>(T reprimand, CancellationToken cancellationToken)
            where T : ReprimandAction, IExpirable
        {
            reprimand.EndedAt = DateTimeOffset.Now;

            var guild = _client.GetGuild(reprimand.GuildId);
            var user = guild.GetUser(reprimand.UserId);
            var moderator = guild.CurrentUser;
            var details = new ModifiedReprimand(user, moderator, ModerationSource.Expiry, "[Reprimand Expired]");

            await UpdateReprimandAsync(reprimand, details, ReprimandStatus.Expired, cancellationToken);
        }

        private async Task UpdateReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details,
            ReprimandStatus status, CancellationToken cancellationToken)
        {
            _db.Update(ModifyReprimand(reprimand, details, status));
            await _db.SaveChangesAsync(cancellationToken);

            await _logging.PublishReprimandAsync(reprimand, details, cancellationToken);
        }

        private async Task<T> PublishReprimandAsync<T>(IRequest<T> request, ReprimandDetails details,
            CancellationToken cancellationToken) where T : ReprimandResult
        {
            var mediator = _scope.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(request, cancellationToken);
            await _logging.PublishReprimandAsync(result, details, cancellationToken);

            return result;
        }
    }
}