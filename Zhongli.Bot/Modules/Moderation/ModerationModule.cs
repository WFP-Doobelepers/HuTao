using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Utilities;
using GuildPermission = Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase
    {
        private readonly ZhongliContext _db;
        private readonly IMediator _mediator;
        private readonly ModerationService _moderationService;

        public ModerationModule(IMediator mediator, ZhongliContext db, ModerationService moderationService)
        {
            _mediator = mediator;
            _db       = db;

            _moderationService = moderationService;
        }

        [Command("ban")]
        [Summary("Ban a user from the current guild.")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task BanAsync(IGuildUser user, uint deleteDays = 1, [Remainder] string? reason = null)
        {
            if (!await _moderationService.TryBanAsync(user, Context.User, deleteDays, reason))
            {
                await ReplyAsync("Ban failed");
                return;
            }

            await ReplyAsync($"{user} has been banned.");
        }

        [Command("kick")]
        [Summary("Kick a user from the current guild.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Kick)]
        public async Task KickAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            await user.KickAsync(reason);

            var details = new ReprimandDetails(user, Context.User, ModerationActionType.Added);
            _db.Add(new Kick(details));
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

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint warnCount = 1, [Remainder] string? reason = null)
        {
            var details = new ReprimandDetails(user, Context.User, ModerationActionType.Added);
            var warning = new Warning(details, warnCount);

            var userEntity = await _db.Users.TrackUserAsync(user);
            var warnings = _db.Set<Warning>()
                .AsQueryable()
                .Where(w => w.GuildId == user.GuildId)
                .Where(w => w.UserId == user.Id)
                .Sum(w => w.Amount);

            userEntity.WarningCount = (int) warnings;
            await _db.SaveChangesAsync();

            await _mediator.Publish(new WarnNotification(user, warning));

            await ReplyAsync(
                $"{user} has been warned {warnCount} times. They have a total of {warnings} warnings.");
        }
    }
}