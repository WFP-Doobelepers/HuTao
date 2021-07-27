using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase
    {
        private readonly ZhongliContext _db;
        private readonly CommandErrorHandler _error;
        private readonly ModerationLoggingService _moderationLogging;
        private readonly ModerationService _moderationService;

        public ModerationModule(
            ZhongliContext db,
            CommandErrorHandler error,
            ModerationService moderationService,
            ModerationLoggingService moderationLogging)
        {
            _db    = db;
            _error = error;

            _moderationService = moderationService;
            _moderationLogging = moderationLogging;
        }

        private ReprimandDetails GetDetails(IGuildUser user, string? reason)
            => new(user, (IGuildUser) Context.User, ModerationSource.Command, reason);

        private ModifiedReprimand GetDetails(IUser user, string? reason)
            => new(user, (IGuildUser) Context.User, ModerationSource.Command, reason);

        [Command("hide")]
        [Summary("Hide a reprimand, this would mean they are not counted towards triggers.")]
        [RequireAuthorization(AuthorizationScope.Auto)]
        public async Task HideReprimandAsync(Guid id, string? reason = null)
        {
            var reprimand = await _db.Set<ReprimandAction>().FindByIdAsync(id);
            if (reprimand is null)
            {
                await _error.AssociateError(Context.Message, "Unable to find reprimand.");
                return;
            }

            var user = await Context.Client.GetUserAsync(reprimand.UserId);
            var details = GetDetails(user, reason);

            await _moderationService.HideReprimandAsync(reprimand, details);
            await ReplyReprimandAsync(reprimand, details);
        }

        [Command("delete")]
        [Summary("Delete a reprimand, this completely removes the data.")]
        [RequireAuthorization(AuthorizationScope.Auto)]
        public async Task DeleteReprimandAsync(Guid id)
        {
            var reprimand = await _db.Set<ReprimandAction>().FindByIdAsync(id);
            if (reprimand is null)
            {
                await _error.AssociateError(Context.Message, "Unable to find reprimand.");
                return;
            }

            var user = await Context.Client.GetUserAsync(reprimand.UserId);
            var details = GetDetails(user, null);

            await _moderationService.DeleteReprimandAsync(details, reprimand);
            await ReplyReprimandAsync(reprimand, details);
        }

        [Command("update")]
        [Summary("Update a reprimand's reason.")]
        [RequireAuthorization(AuthorizationScope.Auto)]
        public async Task UpdateReprimandAsync(Guid id, string? reason = null)
        {
            var reprimand = await _db.Set<ReprimandAction>().FindByIdAsync(id);
            if (reprimand is null)
            {
                await _error.AssociateError(Context.Message, "Unable to find reprimand.");
                return;
            }

            var user = await Context.Client.GetUserAsync(reprimand.UserId);
            var details = GetDetails(user, reason);

            await _moderationService.UpdateReprimandAsync(details, reprimand);
            await ReplyReprimandAsync(reprimand, details);
        }

        [Command("ban")]
        [Summary("Ban a user from the current guild.")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task BanAsync(IGuildUser user, uint deleteDays = 1, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderationService.TryBanAsync(deleteDays, details);
            if (result is null)
                await _error.AssociateError(Context.Message, "Failed to ban user.");
            else
                await ReplyReprimandAsync(result, details);
        }

        [Command("kick")]
        [Summary("Kick a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Kick)]
        public async Task KickAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderationService.TryKickAsync(details);
            if (result is null)
                await _error.AssociateError(Context.Message, "Failed to kick user.");
            else
                await ReplyReprimandAsync(result, details);
        }

        [Command("mute")]
        [Summary("Mute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task MuteAsync(IGuildUser user, TimeSpan? length = null, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderationService.TryMuteAsync(length, details);
            if (result is null)
                await _error.AssociateError(Context.Message, "Failed to mute user.");
            else
                await ReplyReprimandAsync(result, details);
        }

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint amount = 1, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderationService.WarnAsync(amount, details);
            await ReplyReprimandAsync(result, details);
        }

        private async Task ReplyReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details)
        {
            var guild = await reprimand.GetGuildAsync(_db);
            if (!guild.LoggingRules.Options.HasFlag(LoggingOptions.Silent))
            {
                var embed = await _moderationLogging.UpdatedEmbedAsync(details, reprimand);
                await ReplyAsync(embed: embed.Build());
            }
            else
                await Context.Message.DeleteAsync();
        }

        private async Task ReplyReprimandAsync(ReprimandResult result, ReprimandDetails details)
        {
            var guild = await result.Primary.GetGuildAsync(_db);
            if (!guild.LoggingRules.Options.HasFlag(LoggingOptions.Silent))
            {
                var embed = await _moderationLogging.CreateEmbedAsync(details, result);
                await ReplyAsync(embed: embed.Build());
            }
            else
                await Context.Message.DeleteAsync();
        }

        [Command("notice")]
        [Summary("Add a notice to a user. This counts as a minor warning.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoticeAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderationService.NoticeAsync(details);
            await ReplyReprimandAsync(result, details);
        }

        [Command("note")]
        [Summary("Add a note to a user. Notes are always silent.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoteAsync(IGuildUser user, [Remainder] string? note = null)
        {
            var details = GetDetails(user, note);
            await _moderationService.NoteAsync(details);

            await Context.Message.DeleteAsync();
        }
    }
}