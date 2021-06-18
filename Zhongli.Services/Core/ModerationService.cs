using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Reprimands;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core
{
    public class ModerationService
    {
        private readonly ZhongliContext _db;
        private readonly DiscordSocketClient _client;

        public ModerationService(DiscordSocketClient client, ZhongliContext db)
        {
            _client = client;
            _db = db;
        }

        private ConcurrentDictionary<ulong, Mute> ActiveMutes { get; } = new();


        public async Task<bool> TryMuteAsync(IGuildUser user, IUser mod,
            string? reason = null, TimeSpan? length = null,
            CancellationToken cancellationToken = default)
        {
            var guild = await _db.Guilds.FindAsync(user.GuildId);
            var muteRole = guild.MuteRoleId;

            if (muteRole is null || user.HasRole(muteRole.Value))
                return false;

            if (ActiveMutes.TryGetValue(user.Id, out var activeMute))
            {
                activeMute!.EndedAt = DateTimeOffset.UtcNow;
                ActiveMutes.TryRemove(mod.Id, out _);
            }

            var details = new ReprimandDetails(user, mod, ModerationActionType.Added, reason);
            await user.AddRoleAsync(muteRole.Value);
            var mute = new Mute(details, DateTimeOffset.UtcNow, length);

            _db.Add(mute);

            if (mute.TimeLeft is not null)
                _ = EnqueueMuteTimer(user, muteRole.Value, mute.TimeLeft.Value, mute, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task EnqueueMuteTimer(Mute mute, CancellationToken cancellationToken)
        {
            var guildEntity = _db.Guilds.Find(mute.GuildId);
            var muteRoleId = guildEntity.MuteRoleId;
            if (muteRoleId is null)
                return;
        
            var guild = _client.GetGuild(mute.GuildId);
            var user = guild.GetUser(mute.UserId);
        
            await EnqueueMuteTimer(user, muteRoleId.Value, mute.TimeLeft!.Value, mute, cancellationToken);
        }

        public async Task EnqueueMuteTimer(IGuildUser user, ulong roleId, TimeSpan length, Mute mute,
            CancellationToken cancellationToken = default)
        {
            if (!ActiveMutes.TryAdd(mute.UserId, mute))
                return;

            await Task.Delay(length, cancellationToken);
            await user.RemoveRoleAsync(roleId);

            mute.EndedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}