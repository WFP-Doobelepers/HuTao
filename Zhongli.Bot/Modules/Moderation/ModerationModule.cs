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

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandErrorHandler _error;
        private readonly ModerationLoggingService _logging;
        private readonly ModerationService _moderation;
        private readonly ZhongliContext _db;

        public ModerationModule(
            CommandErrorHandler error,
            ZhongliContext db,
            ModerationLoggingService logging,
            ModerationService moderation)
        {
            _error = error;
            _db    = db;

            _logging    = logging;
            _moderation = moderation;
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

        [Command("notice")]
        [Summary("Add a notice to a user. This counts as a minor warning.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoticeAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderation.NoticeAsync(details);
            await ReplyReprimandAsync(result, details);
        }

        [Command("unban")]
        [Summary("Unban a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task UnbanAsync(ulong userId, [Remainder] string? reason = null)
        {
            var user = await Context.Client.Rest.GetUserAsync(userId);
            var details = new ModifiedReprimand(user, (IGuildUser) Context.User, reason);

            var result = await _moderation.TryUnbanAsync(details);
            if (result)
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            else
                await _error.AssociateError(Context.Message, "This user has no ban logs. Forced unban.");
        }

        [Command("unmute")]
        [Summary("Unmute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task UnmuteAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var details = GetDetails(user, reason);
            var result = await _moderation.TryUnmuteAsync(details);

            if (result)
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            else
                await _error.AssociateError(Context.Message, "Unmute failed.");
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

        [Command("warn")]
        [Summary("Warn a user from the current guild once.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public Task WarnAsync(IGuildUser user, [Remainder] string? reason = null)
            => WarnAsync(user, 1, reason);

        private ReprimandDetails GetDetails(IGuildUser user, string? reason)
            => new(user, (IGuildUser) Context.User, reason);

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
    }
}