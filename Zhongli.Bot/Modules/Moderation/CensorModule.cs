using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
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
        private readonly ZhongliContext _db;

        public CensorModule(ZhongliContext db) { _db = db; }

        [Command("add ban")]
        [Summary("A censor that deletes the message and also bans the user.")]
        public async Task AddBanCensorAsync(string pattern, uint deleteDays = 0, TimeSpan? length = null,
            CensorOptions? options = null)
        {
            var censor = new BanCensor(pattern, options, deleteDays, length);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add kick")]
        [Summary("A censor that deletes the message and also kicks the user.")]
        public async Task AddKickCensorAsync(string pattern, CensorOptions? options = null)
        {
            var censor = new KickCensor(pattern, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add mute")]
        [Summary("A censor that deletes the message and mutes the user.")]
        public async Task AddMuteCensorAsync(string pattern, TimeSpan? length = null,
            CensorOptions? options = null)
        {
            var censor = new MuteCensor(pattern, options, length);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add note")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddNoteCensorAsync(string pattern,
            CensorOptions? options = null)
        {
            var censor = new NoteCensor(pattern, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add notice")]
        [Summary("A censor that deletes the message and gives a notice.")]
        public async Task AddNoticeCensorAsync(string pattern,
            CensorOptions? options = null)
        {
            var censor = new NoticeCensor(pattern, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add warning")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddWarningCensorAsync(string pattern, uint count = 1,
            CensorOptions? options = null)
        {
            var censor = new WarningCensor(pattern, options, count);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        private async Task AddCensor(Censor censor)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.ModerationRules.Censors
                .Add(censor.WithModerator(Context));

            await _db.SaveChangesAsync();
        }

        [NamedArgumentType]
        public class CensorOptions : ICensorOptions
        {
            [HelpSummary("Comma separated regex flags.")]
            public RegexOptions Flags { get; set; } = RegexOptions.None;

            [HelpSummary("Time for when the censored reprimand should expire.")]
            public TimeSpan? ExpireAfter { get; set; }

            [HelpSummary("The behavior in which the censor triggers.")]
            public TriggerMode TriggerMode { get; set; } = TriggerMode.Default;

            [HelpSummary("The amount of times the censor should be triggered before taking action.")]
            public uint TriggerAt { get; set; } = 1;
        }
    }
}