using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Utilities;
using GuildPermission = Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase
    {
        private readonly ZhongliContext _db;
        private readonly ModerationService _moderationService;

        public ModerationModule(ZhongliContext db, ModerationService moderationService)
        {
            _db = db;

            _moderationService = moderationService;
        }

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint warnCount = 1, [Remainder] string? reason = null)
        {
            var (action, userEntity) =
                await _moderationService.CreateReprimandAction(user, Context.User, Reprimand.Warning,
                    ModerationActionType.Added, reason);
            var warnEntity = action.ToWarning(warnCount);

            var totalWarnings = userEntity.WarningHistory.Sum(w => w.Amount);
            userEntity.WarningCount = (int) (totalWarnings + warnCount);

            userEntity.WarningHistory.Add(warnEntity);
            userEntity.ReprimandHistory.Add(action);

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
            var (action, userEntity) = await _moderationService.CreateReprimandAction(user, Context.User, Reprimand.Ban,
                ModerationActionType.Added, reason);
            var banAction = action.ToBan(deleteDays);
            userEntity.ReprimandHistory.Add(action);
            userEntity.BanHistory.Add(banAction);

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
            var (action, userEntity) = await _moderationService.CreateReprimandAction(user, Context.User,
                Reprimand.Kick, ModerationActionType.Added, reason);
            var kickAction = action.ToKick();

            userEntity.ReprimandHistory.Add(action);
            userEntity.KickHistory.Add(kickAction);

            await user.KickAsync(reason);
            await _db.SaveChangesAsync();
            await ReplyAsync($"{user} has been kicked.");
        }

        [Command("mute")]
        [Summary("Mute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task MuteAsync(IGuildUser user, TimeSpan? length = null, [Remainder] string? reason = null)
        {
            if (!await _moderationService.TryMuteAsync(user, (IGuildUser) Context.User, reason, length))
            {
                await ReplyAsync("Mute failed");
                return;
            }

            await ReplyAsync($"{user} has been muted.");
        }
    }
}