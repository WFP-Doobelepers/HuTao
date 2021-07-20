using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase
    {
        private readonly ModerationLoggingService _moderationLogging;
        private readonly ModerationService _moderationService;

        public ModerationModule(ModerationService moderationService, ModerationLoggingService moderationLogging)
        {
            _moderationService = moderationService;
            _moderationLogging = moderationLogging;
        }

        private ReprimandDetails GetDetails(IGuildUser user, string? reason)
            => new(user, (IGuildUser) Context.User, ModerationSource.Command, reason);

        [Command("ban")]
        [Summary("Ban a user from the current guild.")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task BanAsync(IGuildUser user, uint deleteDays = 1, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryBanAsync(deleteDays, GetDetails(user, reason)) is not null)
                await ReplyAsync($"{user} has been banned.");
            else
                await ReplyAsync("Ban failed.");
        }

        [Command("kick")]
        [Summary("Kick a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Kick)]
        public async Task KickAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryKickAsync(GetDetails(user, reason)) is not null)
                await ReplyAsync($"{user} has been kicked.");
            else
                await ReplyAsync("Kick failed.");
        }

        [Command("mute")]
        [Summary("Mute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task MuteAsync(IGuildUser user, TimeSpan? length = null, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryMuteAsync(length, GetDetails(user, reason)) is not null)
                await ReplyAsync($"{user} has been muted.");
            else
                await ReplyAsync("Mute failed.");
        }

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint amount = 1, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderationService.WarnAsync(amount, details);
            var embed = await _moderationLogging.CreateEmbedAsync(details, result);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("notice")]
        [Summary("Add a notice to a user. This counts as a minor warning.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoticeAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderationService.NoticeAsync(details);
            var embed = await _moderationLogging.CreateEmbedAsync(details, result);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("note")]
        [Summary("Add a note to a user. This does nothing.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoteAsync(IGuildUser user, [Remainder] string? note = null)
        {
            await _moderationService.NoteAsync(GetDetails(user, note));

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }
}