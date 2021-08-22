using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Interactive;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Censors
{
    [Name("Censors")]
    [Group("censor")]
    [Alias("censors")]
    public class CensorModule : InteractiveEntity<Censor>
    {
        private const string PatternSummary = "The .NET flavor regex pattern to be used.";
        private readonly ZhongliContext _db;

        public CensorModule(CommandErrorHandler error, ZhongliContext db) : base(error, db) { _db = db; }

        protected override string Title { get; } = "Censors";

        [Command("ban")]
        [Summary("A censor that deletes the message and also bans the user.")]
        public async Task AddBanCensorAsync(
            [Summary(PatternSummary)] string pattern,
            [Summary("Amount in days of messages that will be deleted when banned.")]
            uint deleteDays = 0,
            [Summary("The length of the ban. Leave empty for permanent.")]
            TimeSpan? length = null,
            CensorOptions? options = null, Exclusions? exclusions = null)
        {
            var trigger = new BanAction(deleteDays, length);
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, exclusions);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add")]
        [Alias("create")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null, Exclusions? exclusions = null)
        {
            var censor = new Censor(pattern, null, options);

            await AddCensor(censor, exclusions);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("kick")]
        [Summary("A censor that deletes the message and also kicks the user.")]
        public async Task AddKickCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null, Exclusions? exclusions = null)
        {
            var trigger = new KickAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, exclusions);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("mute")]
        [Summary("A censor that deletes the message and mutes the user.")]
        public async Task AddMuteCensorAsync(
            [Summary(PatternSummary)] string pattern,
            [Summary("The length of the mute. Leave empty for permanent.")]
            TimeSpan? length = null,
            CensorOptions? options = null, Exclusions? exclusions = null)
        {
            var trigger = new MuteAction(length);
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, exclusions);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("note")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddNoteCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null, Exclusions? exclusions = null)
        {
            var trigger = new NoteAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, exclusions);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("notice")]
        [Summary("A censor that deletes the message and gives a notice.")]
        public async Task AddNoticeCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null, Exclusions? exclusions = null)
        {
            var trigger = new NoticeAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, exclusions);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("warning")]
        [Alias("warn")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddWarningCensorAsync(
            [Summary(PatternSummary)] string pattern,
            [Summary("The amount of warnings to be given. Defaults to 1.")]
            uint count = 1,
            CensorOptions? options = null, Exclusions? exclusions = null)
        {
            var trigger = new WarningAction(count);
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, exclusions);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command]
        [Alias("list", "view")]
        [Summary("View the censor list.")]
        protected async Task ViewCensorsAsync()
        {
            var collection = await GetCollectionAsync();
            await PagedViewAsync(collection);
        }

        protected override (string Title, StringBuilder Value) EntityViewer(Censor entity)
        {
            var value = new StringBuilder()
                .AppendLine($"▌Pattern: {entity.Pattern}")
                .AppendLine($"▌Options: {entity.Options.Humanize()}")
                .AppendLine($"▌Reprimand: {entity.Reprimand?.Action ?? "None"}")
                .AppendLine($"▌Exclusions: {entity.Exclusions.Humanize()}");

            return (entity.Id.ToString(), value);
        }

        protected override bool IsMatch(Censor entity, string id)
            => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

        protected override async Task RemoveEntityAsync(Censor censor)
        {
            foreach (var reprimand in _db.Set<Reprimand>().ToEnumerable()
                .Where(r => r.TriggerId == censor.Id))
            {
                reprimand.TriggerId = null;
            }

            if (censor.Reprimand is not null)
                _db.Remove(censor.Reprimand);

            _db.RemoveRange(censor.Exclusions);
            _db.Remove(censor.Action);
            _db.Remove(censor);

            await _db.SaveChangesAsync();
        }

        protected override async Task<ICollection<Censor>> GetCollectionAsync()
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            return guild.ModerationRules.Triggers.OfType<Censor>().ToList();
        }

        private async Task AddCensor(Censor censor, Exclusions? exclusions)
        {
            if (exclusions is not null)
                censor.Exclusions = exclusions.ToCriteria();

            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.ModerationRules.Triggers
                .Add(censor.WithModerator(Context));

            await _db.SaveChangesAsync();
        }

        [NamedArgumentType]
        public class CensorOptions : ICensorOptions
        {
            [HelpSummary("Comma separated regex flags.")]
            public RegexOptions Flags { get; set; } = RegexOptions.None;

            [HelpSummary("The behavior in which the reprimand of the censor triggers.")]
            public TriggerMode Mode { get; set; } = TriggerMode.Exact;

            [HelpSummary("The amount of times the censor should be triggered before reprimanding.")]
            public uint Amount { get; set; } = 1;
        }
    }
}