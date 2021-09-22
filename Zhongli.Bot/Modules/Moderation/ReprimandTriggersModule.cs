using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Interactive;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    [Group("trigger")]
    [Alias("triggers")]
    [Summary("Set triggers for moderation tools to automatically activate.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class ReprimandTriggersModule : InteractiveTrigger<ReprimandTrigger>
    {
        private readonly CommandErrorHandler _error;
        private readonly ZhongliContext _db;

        public ReprimandTriggersModule(CommandErrorHandler error, ZhongliContext db, ModerationService moderation)
            : base(error, db, moderation)
        {
            _error = error;
            _db    = db;
        }

        [Command("ban")]
        [Summary("Ban a user from the current guild on activating a trigger")]
        public async Task BanTriggerAsync(uint amount, TriggerSource source,
            uint deleteDays = 0, TimeSpan? length = null,
            TriggerMode mode = TriggerMode.Exact)
        {
            var action = new BanAction(deleteDays, length);
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("kick")]
        [Summary("Kick a user from the current guild on activating a trigger")]
        public async Task KickTriggerAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Exact)
        {
            var action = new KickAction();
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("mute")]
        [Summary("Mute a user in the current guild on activating a trigger")]
        public async Task MuteTriggerAsync(uint amount, TriggerSource source, TimeSpan? length = null,
            TriggerMode mode = TriggerMode.Exact)
        {
            var action = new MuteAction(length);
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("note")]
        [Summary("Add a note to a user. Notes are always silent.")]
        public async Task NoteTriggerAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Exact)
        {
            var action = new NoteAction();
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("notice")]
        [Summary("Add a notice to a user. This counts as a minor warning.")]
        public async Task NoticeTriggerAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Exact)
        {
            var action = new NoticeAction();
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("warn")]
        [Summary("Warn a user in the current guild.")]
        public async Task WarnTriggerAsync(uint amount, TriggerSource source,
            uint count = 1, TriggerMode mode = TriggerMode.Exact)
        {
            var action = new WarningAction(count);
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("reprimands")]
        [Alias("history")]
        [Summary("Shows associated reprimands of this trigger.")]
        protected async Task ViewAssociatedReprimandsAsync(string id,
            [Summary("Leave empty to show everything.")]
            InfractionType type = InfractionType.All)
        {
            var trigger = await TryFindEntityAsync(id);

            if (trigger is null)
            {
                await _error.AssociateError(Context.Message, EmptyMatchMessage);
                return;
            }

            var reprimands = await _db.Set<Reprimand>().AsAsyncEnumerable()
                .Where(r => r.TriggerId == trigger.Id)
                .ToListAsync();

            var author = new EmbedAuthorBuilder().WithGuildAsAuthor(Context.Guild);
            await PagedViewAsync(reprimands.OfType(type),
                r => (r.GetTitle(), r.GetReprimandDetails()),
                "Reprimands", author);
        }

        [Command]
        [Alias("list", "view")]
        [Summary("View the reprimand trigger list.")]
        protected override Task ViewEntityAsync() => base.ViewEntityAsync();

        protected override (string Title, StringBuilder Value) EntityViewer(ReprimandTrigger trigger)
        {
            var content = new StringBuilder()
                .AppendLine($"▌Action: {trigger.Reprimand.Action}")
                .AppendLine($"▌Trigger: {trigger.GetTriggerDetails()}")
                .AppendLine($"▌▌Active: {trigger.IsActive}")
                .AppendLine($"▌▌Modified by: {trigger.GetModerator()}");

            return ($"{trigger.Reprimand.GetTitle()}: {trigger.Id}", content);
        }

        protected override bool IsMatch(ReprimandTrigger entity, string id)
            => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

        protected override async Task RemoveEntityAsync(ReprimandTrigger entity)
        {
            var triggerHasReprimand = _db.Set<Reprimand>()
                .Any(r => r.TriggerId == entity.Id);

            if (triggerHasReprimand)
                entity.IsActive = false;
            else
                _db.Remove(entity);

            await _db.SaveChangesAsync();
        }

        protected override async Task<ICollection<ReprimandTrigger>> GetCollectionAsync()
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var rules = guild.ModerationRules;

            return rules.Triggers.OfType<ReprimandTrigger>().ToArray();
        }

        private async Task TryAddTriggerAsync(ReprimandAction action, uint amount, TriggerSource source,
            TriggerMode mode)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var rules = guild.ModerationRules;

            var options = new TriggerOptions(amount, source, mode);
            var trigger = new ReprimandTrigger(options, options.Source, action);

            var existing = rules.Triggers.OfType<ReprimandTrigger>()
                .Where(t => t.IsActive)
                .FirstOrDefault(t => t.Source == options.Source && t.Amount == trigger.Amount);

            if (existing is not null) await RemoveEntityAsync(existing);

            rules.Triggers.Add(trigger.WithModerator(Context));
            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        private class TriggerOptions : ITrigger
        {
            public TriggerOptions(uint amount, TriggerSource source, TriggerMode mode)
            {
                Mode   = mode;
                Amount = amount;
                Source = source;
            }

            public TriggerSource Source { get; }

            public TriggerMode Mode { get; set; }

            public uint Amount { get; set; }
        }
    }
}