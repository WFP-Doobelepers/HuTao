using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Addons.Interactive.Paginator;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Interactive.Functions;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Censor")]
    [Group("censor")]
    public class CensorModule : InteractiveBase
    {
        private const string PatternSummary = "The .NET flavor regex pattern to be used.";
        private readonly CommandErrorHandler _error;
        private readonly ZhongliContext _db;

        public CensorModule(ZhongliContext db, CommandErrorHandler error)
        {
            _db    = db;
            _error = error;
        }

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

        [Command("exclude")]
        [Summary("Exclude the set criteria globally in all censors.")]
        public async Task ExcludeAsync(Exclusions exclusions)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.ModerationRules.CensorExclusions.AddCriteria(exclusions);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("exclusions")]
        [Summary("View the configured censor exclusions.")]
        public async Task ExclusionsViewAsync()
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);

            var rules = guild.ModerationRules.CensorExclusions
                .Select(c => new EmbedFieldBuilder()
                    .WithName($"{c.Id}")
                    .WithValue(c.ToString()));

            var paginated = new PaginatedMessage
            {
                Pages  = rules,
                Author = new EmbedAuthorBuilder().WithGuildAsAuthor(Context.Guild),
                Options = new PaginatedAppearanceOptions
                {
                    FieldsPerPage = 8
                }
            };

            await PagedReplyAsync(paginated);
        }

        [Command("include")]
        [Alias("delete")]
        [Summary("Remove an exclusion.")]
        public async Task RemovePermissionAsync(Guid id, [Remainder] string? reason)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);

            var reprimand = guild.ModerationRules.CensorExclusions.FirstOrDefault(c => c.Id == id);
            await RemoveCriterionAsync(reprimand);
        }

        [HiddenFromHelp]
        [Command("include")]
        public async Task RemovePermissionAsync(string id, [Remainder] string? reason)
        {
            var reprimand = await TryFindCensorExclusions(id);
            await RemoveCriterionAsync(reprimand);
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

        private async Task RemoveCriterionAsync(Criterion? criterion)
        {
            if (criterion is null)
            {
                await _error.AssociateError(Context.Message,
                    "Unable to find authorization group. Provide at least 2 characters.");
                return;
            }

            _db.Remove(criterion);
            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        private async Task<Criterion?> TryFindCensorExclusions(string id,
            CancellationToken cancellationToken = default)
        {
            if (id.Length < 2)
                return null;

            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild, cancellationToken);
            var group = guild.ModerationRules.CensorExclusions
                .Where(r => r.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (group.Count <= 1)
                return group.Count == 1 ? group.First() : null;

            var embed = new EmbedBuilder()
                .WithTitle("Multiple censor exclusions found. Reply with the number of the group that you want.")
                .AddItemsIntoFields("Censors", group,
                    (criterion, i) => $"{i}. {criterion.Id}: {criterion}");

            await ReplyAsync(embed: embed.Build());

            var containsCriterion = new FuncCriterion(m =>
                int.TryParse(m.Content, out var selection)
                && selection < group.Count && selection > -1);

            var selected = await NextMessageAsync(containsCriterion, token: cancellationToken);
            return selected is null ? null : group.ElementAtOrDefault(int.Parse(selected.Content));
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

        [NamedArgumentType]
        public class Exclusions : ICriteriaOptions
        {
            [HelpSummary("The permissions that the user must have.")]
            public GuildPermission Permission { get; set; } = GuildPermission.None;

            [HelpSummary("The text or category channels that will be excluded.")]
            public IEnumerable<IGuildChannel>? Channels { get; set; }

            [HelpSummary("The users that are excluded.")]
            public IEnumerable<IGuildUser>? Users { get; set; }

            [HelpSummary("The roles that are excluded.")]
            public IEnumerable<IRole>? Roles { get; set; }
        }
    }
}