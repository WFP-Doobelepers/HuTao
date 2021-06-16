using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Reprimands;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core
{
    public class ModerationService : INotificationHandler<ReadyNotification>
    {
        private readonly DiscordSocketClient _client;
        private readonly ZhongliContext _db;
        private Task? _mutesProcessor;

        public ModerationService(DiscordSocketClient client, ZhongliContext db)
        {
            _client = client;
            _db     = db;
        }

        private ConcurrentDictionary<ulong, Mute> ActiveMutes { get; } = new();

        public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            if (_mutesProcessor is not null)
                return;

            _mutesProcessor = Task.Factory.StartNew(ProcessMutes, cancellationToken, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public async Task<bool> TryMuteAsync(IGuildUser user, IGuildUser mod, string? reason = null,
            TimeSpan? length = null,
            CancellationToken cancellationToken = default)
        {
            var (action, userEntity) =
                await CreateReprimandAction(user, mod, Reprimand.Mute, ModerationActionType.Added, reason);
            var muteRole = action.Guild.MuteRoleId;

            if (muteRole is null || user.HasRole(muteRole.Value))
                return false;

            if (ActiveMutes.TryGetValue(user.Id, out var activeMute))
            {
                activeMute!.EndedAt = DateTimeOffset.UtcNow;
                ActiveMutes.TryRemove(mod.Id, out _);
            }

            await user.AddRoleAsync(muteRole.Value);

            var muteAction = action.ToMute(length);
            userEntity.ReprimandHistory.Add(action);
            userEntity.MuteHistory.Add(muteAction);

            if (muteAction.TimeLeft is not null)
                _ = EnqueueMuteTimer(user, muteRole.Value, muteAction.TimeLeft.Value, muteAction);

            await _db.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task ProcessMutes()
        {
            while (true)
            {
                var now = DateTimeOffset.Now;
                var activeMutes = await _db.Set<Mute>()
                    .AsAsyncEnumerable()
                    .Where(m => m.EndedAt == null)
                    .Where(m => m.StartedAt + m.Length > now)
                    .Where(m => m.StartedAt + m.Length - now < TimeSpan.FromMinutes(10))
                    .ToListAsync();

                foreach (var mute in activeMutes)
                {
                    _ = EnqueueMuteTimer(mute);
                }

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }

        private async Task EnqueueMuteTimer(Mute mute)
        {
            var muteRoleId = mute.User.Guild.MuteRoleId;
            if (muteRoleId is null)
                return;

            var guild = _client.GetGuild(mute.User.GuildId);
            var user = guild.GetUser(mute.User.Id);

            await EnqueueMuteTimer(user, muteRoleId.Value, mute.TimeLeft!.Value, mute);
        }

        private async Task EnqueueMuteTimer(IGuildUser user, ulong roleId, TimeSpan length, Mute mute)
        {
            if (!ActiveMutes.TryAdd(mute.User.Id, mute))
                return;

            await Task.Delay(length);
            await user.RemoveRoleAsync(roleId);

            mute.EndedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<(ReprimandAction, GuildUserEntity)> CreateReprimandAction(IGuildUser user, IGuildUser mod,
            Reprimand reprimand,
            ModerationActionType type, string? reason = null)
        {
            var userEntity = await _db.Users.FindAsync(user.Id) ?? _db.Add(new GuildUserEntity(user)).Entity;
            var modEntity = await _db.Users.FindAsync(mod.Id);
            var guildEntity = await _db.Guilds.FindAsync(mod.GuildId);

            return (new ReprimandAction
            {
                Reprimand = reprimand,
                Type      = type,

                User      = userEntity,
                Moderator = modEntity,
                Guild     = guildEntity,

                Reason = reason,
                Date   = DateTimeOffset.UtcNow
            }, userEntity);
        }

        public Task<(ReprimandAction, GuildUserEntity)> CreateReprimandAction(IGuildUser user, IUser mod,
            Reprimand reprimand,
            ModerationActionType added, string? reason) =>
            CreateReprimandAction(user, (IGuildUser) mod, reprimand, added, reason);
    }
}