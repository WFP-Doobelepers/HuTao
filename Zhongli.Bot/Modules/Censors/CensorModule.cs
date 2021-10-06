using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Interactive;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Censors
{
    [Name("Censor")]
    [Group("censor")]
    [Alias("censors")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class CensorModule : InteractiveTrigger<Censor>
    {
        private const string PatternSummary = "The .NET flavor regex pattern to be used.";
        private readonly ZhongliContext _db;

        public CensorModule(CommandErrorHandler error, ZhongliContext db, ModerationService moderation)
            : base(error, db, moderation)
        {
            _db = db;
        }

        protected override string Title { get; } = "Censors";

        [Command("ban")]
        [Summary("A censor that deletes the message and also bans the user.")]
        public async Task AddBanCensorAsync(
            [Summary(PatternSummary)] string pattern,
            [Summary("Amount in days of messages that will be deleted when banned.")]
            uint deleteDays = 0,
            [Summary("The length of the ban. Leave empty for permanent.")]
            TimeSpan? length = null,
            CensorOptions? options = null)
        {
            var trigger = new BanAction(deleteDays, length);
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, options);
            await ReplyCensorAsync(censor);
        }

        [Command("add")]
        [Alias("create")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null)
        {
            var censor = new Censor(pattern, null, options);

            await AddCensor(censor, options);
            await ReplyCensorAsync(censor);
        }

        [Command("kick")]
        [Summary("A censor that deletes the message and also kicks the user.")]
        public async Task AddKickCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null)
        {
            var trigger = new KickAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, options);
            await ReplyCensorAsync(censor);
        }

        [Command("mute")]
        [Summary("A censor that deletes the message and mutes the user.")]
        public async Task AddMuteCensorAsync(
            [Summary(PatternSummary)] string pattern,
            [Summary("The length of the mute. Leave empty for permanent.")]
            TimeSpan? length = null,
            CensorOptions? options = null)
        {
            var trigger = new MuteAction(length);
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, options);
            await ReplyCensorAsync(censor);
        }

        [Command("note")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddNoteCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null)
        {
            var trigger = new NoteAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, options);
            await ReplyCensorAsync(censor);
        }

        [Command("notice")]
        [Summary("A censor that deletes the message and gives a notice.")]
        public async Task AddNoticeCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null)
        {
            var trigger = new NoticeAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, options);
            await ReplyCensorAsync(censor);
        }

        [Command("warning")]
        [Alias("warn")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddWarningCensorAsync(
            [Summary(PatternSummary)] string pattern,
            [Summary("The amount of warnings to be given. Defaults to 1.")]
            uint count = 1,
            CensorOptions? options = null)
        {
            var trigger = new WarningAction(count);
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor, options);
            await ReplyCensorAsync(censor);
        }

        [Command("test")]
        [Alias("testword")]
        [Summary("Test whether a word is in the list of censors or not.")]
        public async Task TestCensorAsync(string word)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var matches = guild.ModerationRules.Triggers.OfType<Censor>()
                .Where(c => c.Regex().IsMatch(word));
            
            if (matches.Any())
                await PagedViewAsync(matches);
            else
                await ReplyAsync("No matches found.");
        }

        [Command]
        [Alias("list", "view")]
        [Summary("View the censor list.")]
        protected override Task ViewEntityAsync() => base.ViewEntityAsync();

        protected override (string Title, StringBuilder Value) EntityViewer(Censor censor)
        {
            var value = new StringBuilder()
                .AppendLine($"▌Pattern: {Format.Code(censor.Pattern)}")
                .AppendLine($"▌Options: {censor.Options.Humanize()}")
                .AppendLine($"▌Reprimand: {censor.Reprimand?.Action ?? "None"}")
                .AppendLine($"▌Exclusions: {censor.Exclusions.Humanize()}")
                .AppendLine($"▉ Active: {censor.IsActive}")
                .AppendLine($"▉ Modified by: {censor.GetModerator()}");

            return (censor.Id.ToString(), value);
        }

        protected override bool IsMatch(Censor entity, string id)
            => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

        protected override async Task<ICollection<Censor>> GetCollectionAsync()
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            return guild.ModerationRules.Triggers.OfType<Censor>().ToList();
        }

        private async Task AddCensor(Censor censor, ICriteriaOptions? exclusions)
        {
            if (exclusions is not null)
                censor.Exclusions = exclusions.ToCriteria();

            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.ModerationRules.Triggers
                .Add(censor.WithModerator(Context));

            await _db.SaveChangesAsync();
        }

        private async Task ReplyCensorAsync(Censor censor)
        {
            var (title, value) = EntityViewer(censor);

            var embed = new EmbedBuilder()
                .WithTitle("Censor added successfully.")
                .WithDescription(value.ToString())
                .AddField("ID", title)
                .WithColor(Color.Green)
                .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

            await ReplyAsync(embed: embed.Build());
        }

        [NamedArgumentType]
        public class CensorOptions : ICensorOptions, ICriteriaOptions
        {
            [HelpSummary("Silently match and do not delete the message.")]
            public bool Silent { get; set; } = false;

            [HelpSummary("Comma separated regex flags.")]
            public RegexOptions Flags { get; set; } = RegexOptions.None;

            [HelpSummary("The permissions that the user must have.")]
            public GuildPermission Permission { get; set; } = GuildPermission.None;

            [HelpSummary("The text or category channels that will be excluded.")]
            public IEnumerable<IGuildChannel>? Channels { get; set; }

            [HelpSummary("The users that are excluded.")]
            public IEnumerable<IGuildUser>? Users { get; set; }

            [HelpSummary("The roles that are excluded.")]
            public IEnumerable<IRole>? Roles { get; set; }

            [HelpSummary("The behavior in which the reprimand of the censor triggers.")]
            public TriggerMode Mode { get; set; } = TriggerMode.Exact;

            [HelpSummary("The amount of times the censor should be triggered before reprimanding.")]
            public uint Amount { get; set; } = 1;
        }
    }
}