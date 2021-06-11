using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation;

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
        public async Task WarnAsync(IGuildUser user, uint warnCount = 1, [Remainder] string? reason = null)
        {
            var userEntity = await _db.Users.FindAsync(user.Id) ?? _db.Add(new GuildUserEntity(user)).Entity;
            var modEntity = await _db.Users.FindAsync(Context.User.Id);
            var guildEntity = await _db.Guilds.FindAsync(Context.Guild.Id);

            await _db.SaveChangesAsync();

            userEntity.WarningCount += (int) warnCount;
            var warnEntity = new Warning
            {
                User      = userEntity,
                Moderator = modEntity,
                Guild     = guildEntity,

                Amount = warnCount,
                Reason = reason,

                Date  = DateTimeOffset.UtcNow,
            };

            _db.Update(userEntity);
            _db.Add(warnEntity);

            await _db.SaveChangesAsync();

            await ReplyAsync($"{user} has been warned {warnCount} times. They have a total of {userEntity.WarningCount} warnings.");
        }
    }
}