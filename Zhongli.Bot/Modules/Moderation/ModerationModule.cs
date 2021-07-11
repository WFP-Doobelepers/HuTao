using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Preconditions;

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
            if (!await _moderationService.TryBanAsync(user, (IGuildUser) Context.User, deleteDays, reason))
            {
                await ReplyAsync("Ban failed.");
                ;
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
            if (!await _moderationService.TryKickAsync(user, (IGuildUser) Context.User,
                reason)) await ReplyAsync("Kick failed.");

            await ReplyAsync($"{user} has been kicked.");
        }

        [Command("mute")]
        [Summary("Mute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task MuteAsync(IGuildUser user, TimeSpan? length = null, [Remainder] string? reason = null)
        {
            if (!await _moderationService.TryMuteAsync(user, (IGuildUser) Context.User, length, reason))
            {
                await ReplyAsync("Mute failed.");
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
            var warnings = await _moderationService.WarnAsync(user, (IGuildUser) Context.User, warnCount, reason);

            await ReplyAsync($"{user} has been warned {warnCount} times. They have a total of {warnings} warnings.");
        }
    }
}