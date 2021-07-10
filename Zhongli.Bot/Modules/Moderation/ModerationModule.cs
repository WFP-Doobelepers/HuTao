using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Preconditions;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase
    {
        private readonly ModerationService _moderationService;

        public ModerationModule(ModerationService moderationService) { _moderationService = moderationService; }

        private ReprimandDetails GetDetails(IGuildUser user, string? reason)
            => new(user, (IGuildUser) Context.User, ModerationActionType.Added, reason);

        [Command("ban")]
        [Summary("Ban a user from the current guild.")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task BanAsync(IGuildUser user, uint deleteDays = 1, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryBanAsync(deleteDays, GetDetails(user, reason)))
                await ReplyAsync($"{user} has been banned.");
            else
                await ReplyAsync("Ban failed.");
        }

        [Command("kick")]
        [Summary("Kick a user from the current guild.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Kick)]
        public async Task KickAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryKickAsync(GetDetails(user, reason)))
                await ReplyAsync($"{user} has been kicked.");
            else
                await ReplyAsync("Kick failed.");
        }

        [Command("mute")]
        [Summary("Mute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task MuteAsync(IGuildUser user, TimeSpan? length = null, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryMuteAsync(GetDetails(user, reason), length))
                await ReplyAsync($"{user} has been muted.");
            else
                await ReplyAsync("Mute failed.");
        }

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint warnCount = 1, [Remainder] string? reason = null)
        {
            var warnings = await _moderationService.WarnAsync(warnCount, GetDetails(user, reason));

            await ReplyAsync($"{user} has been warned {warnCount} times. They have a total of {warnings} warnings.");
        }
    }
}