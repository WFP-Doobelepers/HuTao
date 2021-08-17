using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Censor")]
    [Group("censor")]
    public class CensorModule : ModuleBase
    {
        private const string PatternSummary = "The .NET flavor regex pattern to be used.";
        private readonly ZhongliContext _db;

        public CensorModule(ZhongliContext db) { _db = db; }

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

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("kick")]
        [Summary("A censor that deletes the message and also kicks the user.")]
        public async Task AddKickCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null)
        {
            var trigger = new KickAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
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

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("note")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddNoteCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null)
        {
            var trigger = new NoteAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("notice")]
        [Summary("A censor that deletes the message and gives a notice.")]
        public async Task AddNoticeCensorAsync(
            [Summary(PatternSummary)] string pattern,
            CensorOptions? options = null)
        {
            var trigger = new NoticeAction();
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("warning")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddWarningCensorAsync(
            [Summary(PatternSummary)] string pattern,
            [Summary("The amount of warnings to be given. Defaults to 1.")]
            uint count = 1,
            CensorOptions? options = null)
        {
            var trigger = new WarningAction(count);
            var censor = new Censor(pattern, trigger, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        private async Task AddCensor(Censor censor)
        {
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

            [HelpSummary("The behavior in which the reprimand action triggers.")]
            public TriggerMode Mode { get; set; } = TriggerMode.Exact;

            [HelpSummary("The amount of times the censor should be triggered before taking action.")]
            public uint Amount { get; set; } = 1;
        }
    }
}