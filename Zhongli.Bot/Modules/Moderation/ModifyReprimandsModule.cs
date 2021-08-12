using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Interactive;
using Zhongli.Services.Interactive.Functions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Reprimand Modification")]
    [Summary("Modification of reprimands. Provide a partial ID with at least the 2 starting characters.")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public class ModifyReprimandsModule : InteractivePromptBase
    {
        private readonly CommandErrorHandler _error;
        private readonly ModerationLoggingService _logging;
        private readonly ModerationService _moderation;
        private readonly ZhongliContext _db;

        public ModifyReprimandsModule(
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

        [Command("hide")]
        [Summary("Hide a reprimand, this would mean they are not counted towards triggers.")]
        public async Task HideReprimandAsync(Guid id, [Remainder] string? reason = null)
        {
            var reprimand = await _db.Set<ReprimandAction>().FindByIdAsync(id);
            await ModifyReprimandAsync(reprimand, _moderation.HideReprimandAsync, reason);
        }

        [HiddenFromHelp]
        [Command("hide")]
        public async Task HideReprimandAsync(string id, [Remainder] string? reason = null)
        {
            var reprimand = await TryFindReprimandAsync(id);
            await ModifyReprimandAsync(reprimand, _moderation.HideReprimandAsync, reason);
        }

        [Command("delete")]
        [Summary("Delete a reprimand, this completely removes the data.")]
        public async Task DeleteReprimandAsync(Guid id)
        {
            var reprimand = await _db.Set<ReprimandAction>().FindByIdAsync(id);
            await ModifyReprimandAsync(reprimand, _moderation.DeleteReprimandAsync);
        }

        [HiddenFromHelp]
        [Command("delete")]
        public async Task DeleteReprimandAsync(string id)
        {
            var reprimand = await TryFindReprimandAsync(id);
            await ModifyReprimandAsync(reprimand, _moderation.DeleteReprimandAsync);
        }

        [Command("update")]
        [Summary("Update a reprimand's reason.")]
        public async Task UpdateReprimandAsync(Guid id, [Remainder] string? reason = null)
        {
            var reprimand = await _db.Set<ReprimandAction>().FindByIdAsync(id);
            await ModifyReprimandAsync(reprimand, _moderation.UpdateReprimandAsync, reason);
        }

        [HiddenFromHelp]
        [Command("update")]
        public async Task UpdateReprimandAsync(string id, [Remainder] string? reason = null)
        {
            var reprimand = await TryFindReprimandAsync(id);
            await ModifyReprimandAsync(reprimand, _moderation.UpdateReprimandAsync, reason);
        }

        private ModifiedReprimand GetDetails(IUser user, string? reason)
            => new(user, (IGuildUser) Context.User, ModerationSource.Command, reason);

        private async Task ModifyReprimandAsync(ReprimandAction? reprimand,
            UpdateReprimandDelegate update, string? reason = null)
        {
            if (reprimand is null || reprimand.GuildId != Context.Guild.Id)
            {
                await _error.AssociateError(Context.Message,
                    "Unable to find reprimand. Provide at least 2 characters.");
                return;
            }

            var user = Context.Client.GetUser(reprimand.UserId);
            var details = GetDetails(user, reason);

            await update(reprimand, details);
            await ReplyReprimandAsync(reprimand, details);
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

        private async Task<ReprimandAction?> TryFindReprimandAsync(string id,
            CancellationToken cancellationToken = default)
        {
            if (id.Length < 2)
                return null;

            var reprimands = await _db.Set<ReprimandAction>().ToAsyncEnumerable()
                .Where(r => r.Guild?.Id == Context.Guild.Id)
                .Where(r => r.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase))
                .ToListAsync(cancellationToken);

            if (reprimands.Count <= 1)
                return reprimands.Count == 1 ? reprimands.First() : null;

            var embed = new EmbedBuilder()
                .WithTitle("Multiple reprimands found. Reply with the number of the reprimand that you want.")
                .AddLinesIntoFields("Reprimands", reprimands,
                    (r, i) => $"{Format.Code($"{i}")}: {ModerationLoggingService.GetTitle(r)}");

            await ReplyAsync(embed: embed.Build());

            var containsCriterion = new FuncCriterion(m =>
                int.TryParse(m.Content, out var selection)
                && selection < reprimands.Count && selection > -1);

            var selected = await NextMessageAsync(containsCriterion, token: cancellationToken);
            return selected is null ? null : reprimands.ElementAtOrDefault(int.Parse(selected.Content));
        }

        private delegate Task UpdateReprimandDelegate(ReprimandAction reprimand, ModifiedReprimand details,
            CancellationToken cancellationToken = default);
    }
}