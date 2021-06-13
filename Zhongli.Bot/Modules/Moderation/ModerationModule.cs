using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation;
using Zhongli.Services.Utilities;
using GuildPermission = Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase
    {
        private readonly ZhongliContext _db;

        public ModerationModule(ZhongliContext db) { _db = db; }

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint warnCount = 1, [Remainder] string? reason = null)
        {
            var userEntity = await _db.Users.FindAsync(user.Id) ?? _db.Add(new GuildUserEntity(user)).Entity;
            var modEntity = await _db.Users.FindAsync(Context.User.Id);
            var guildEntity = await _db.Guilds.FindAsync(Context.Guild.Id);

            await _db.SaveChangesAsync();

            userEntity.WarningCount = (int) (userEntity.WarningHistory.Sum(w => w.Amount) + warnCount);
            var warnEntity = new Warning
            {
                User      = userEntity,
                Moderator = modEntity,
                Guild     = guildEntity,

                Amount = warnCount,
                Reason = reason,
                Date   = DateTimeOffset.UtcNow
            };
            var actionEntity = ReprimandAction.FromWarning(warnEntity);

            userEntity.WarningHistory.Add(warnEntity);
            userEntity.ReprimandHistory.Add(actionEntity);
            _db.Update(userEntity);

            await _db.SaveChangesAsync();

            await ReplyAsync(
                $"{user} has been warned {warnCount} times. They have a total of {userEntity.WarningCount} warnings.");
        }

        [Command("ban")]
        [Summary("Ban a user from the current guild.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task BanAsync(IGuildUser user, uint deleteDays = 1, [Remainder] string? reason = null)
        {
            var userEntity = await _db.Users.FindAsync(user.Id) ?? _db.Add(new GuildUserEntity(user)).Entity;
            await _db.SaveChangesAsync();

            userEntity.ReprimandHistory
                .Add(await CreateReprimandAction(user, Reprimand.Ban, reason));

            _db.Update(userEntity);

            await user.BanAsync((int) deleteDays, reason);
            await _db.SaveChangesAsync();
            await ReplyAsync($"{user} has been banned.");
        }

        [Command("kick")]
        [Summary("Kick a user from the current guild.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Kick)]
        public async Task KickAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var userEntity = await _db.Users.FindAsync(user.Id) ?? _db.Add(new GuildUserEntity(user)).Entity;
            await _db.SaveChangesAsync();

            userEntity.ReprimandHistory
                .Add(await CreateReprimandAction(user, Reprimand.Kick, reason));
            _db.Update(userEntity);

            await user.KickAsync(reason);
            await _db.SaveChangesAsync();
            await ReplyAsync($"{user} has been kicked.");
        }

        private async Task<ReprimandAction> CreateReprimandAction(IGuildUser user, Reprimand reprimand, string? reason)
        {
            var userEntity = await _db.Users.FindAsync(user.Id) ?? _db.Add(new GuildUserEntity(user)).Entity;
            var modEntity = await _db.Users.FindAsync(Context.User.Id);
            var guildEntity = await _db.Guilds.FindAsync(Context.Guild.Id);

            return new ReprimandAction
            {
                Reprimand = reprimand,

                User      = userEntity,
                Moderator = modEntity,
                Guild     = guildEntity,

                Reason = reason,
                Date   = DateTimeOffset.UtcNow
            };
        }
    }
}