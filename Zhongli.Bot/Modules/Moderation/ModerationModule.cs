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
        private readonly ModerationLoggingService _logging;
        private readonly ModerationService _moderation;

        public ModerationModule(ZhongliContext db, CommandErrorHandler error,
            ModerationService moderation, ModerationLoggingService logging)
        {
            _db    = db;
            _error = error;

            _moderation = moderation;
            _logging    = logging;
        }

        private ReprimandDetails GetDetails(IGuildUser user, string? reason)
            => new(user, (IGuildUser) Context.User, ModerationSource.Command, reason);

        private ModifiedReprimand GetDetails(IUser user, string? reason)
            => new(user, (IGuildUser) Context.User, ModerationSource.Command, reason);

        [Command("hide")]
        [Summary("Hide a reprimand, this would mean they are not counted towards triggers.")]
        [RequireAuthorization(AuthorizationScope.Moderator)]
        public async Task HideReprimandAsync(Guid id, [Remainder] string? reason = null)
        {
            var reprimand = await _db.Set<ReprimandAction>().FindByIdAsync(id);
            if (reprimand is null)
            {
                await _error.AssociateError(Context.Message, "Unable to find reprimand.");
                return;
            }

            var user = await Context.Client.GetUserAsync(reprimand.UserId);
            var details = GetDetails(user, reason);

            await _moderation.HideReprimandAsync(reprimand, details);
            await ReplyReprimandAsync(reprimand, details);
        }

        [Command("delete")]
        [Summary("Delete a reprimand, this completely removes the data.")]
        [RequireAuthorization(AuthorizationScope.Moderator)]
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

            await _moderation.DeleteReprimandAsync(reprimand, details);
            await ReplyReprimandAsync(reprimand, details);
        }

        [Command("update")]
        [Summary("Update a reprimand's reason.")]
        [RequireAuthorization(AuthorizationScope.Moderator)]
        public async Task UpdateReprimandAsync(Guid id, [Remainder] string? reason = null)
        {
            var reprimand = await _db.Set<ReprimandAction>().FindByIdAsync(id);
            if (reprimand is null)
            {
                await _error.AssociateError(Context.Message, "Unable to find reprimand.");
                return;
            }

            var user = await Context.Client.GetUserAsync(reprimand.UserId);
            var details = GetDetails(user, reason);

            await _moderation.UpdateReprimandAsync(reprimand, details);
            await ReplyReprimandAsync(reprimand, details);
        }

        [Command("ban")]
        [Summary("Ban a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task BanAsync(IGuildUser user, uint deleteDays = 1, TimeSpan? length = null,
            [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderation.TryBanAsync(deleteDays, length, details);
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
            var result = await _moderation.TryKickAsync(details);
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
            var result = await _moderation.TryMuteAsync(length, details);
            if (result is null)
            {
                await _error.AssociateError(Context.Message, "Failed to mute user. " +
                    "Either the user is already muted or there is no mute role configured. " +
                    "Configure the mute role by running the 'configure mute' command.");
            }
            else
                await ReplyReprimandAsync(result, details);
        }

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint amount = 1, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderation.WarnAsync(amount, details);
            await ReplyReprimandAsync(result, details);
        }

        private async Task ReplyReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details)
        {
            var guild = await reprimand.GetGuildAsync(_db);
            if (!guild.LoggingRules.Options.HasFlag(LoggingOptions.Silent))
            {
                var embed = await _logging.UpdatedEmbedAsync(reprimand, details);
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
                var embed = await _logging.CreateEmbedAsync(result, details);
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
            var result = await _moderation.NoticeAsync(details);
            await ReplyReprimandAsync(result, details);
        }

        [Command("note")]
        [Summary("Add a note to a user. Notes are always silent.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoteAsync(IGuildUser user, [Remainder] string? note = null)
        {
            var details = GetDetails(user, note);
            await _moderation.NoteAsync(details);

            await Context.Message.DeleteAsync();
        }
    }
}