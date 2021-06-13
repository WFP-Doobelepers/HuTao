using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Reprimands;
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
            await _db.SaveChangesAsync();

            var totalWarnings = userEntity.WarningHistory.Sum(w => w.Amount);
            userEntity.WarningCount = (int) (totalWarnings + warnCount);

            var actionEntity = await CreateReprimandAction(user, Reprimand.Warning, reason);
            var warnEntity = actionEntity.ToWarning(warnCount);

            userEntity.WarningHistory.Add(warnEntity);
            userEntity.ReprimandHistory.Add(actionEntity);

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

            var action = await CreateReprimandAction(user, Reprimand.Ban, reason);
            userEntity.ReprimandHistory.Add(action);

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

            var action = await CreateReprimandAction(user, Reprimand.Kick, reason);
            userEntity.ReprimandHistory.Add(action);

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