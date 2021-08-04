using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;

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
        public async Task AddBanCensorAsync(string pattern, TimeSpan? length = null,
            RegexOptions options = RegexOptions.None,
            uint deleteDays = 0)
        {
            var censor = new BanCensor(deleteDays, length, pattern, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add kick")]
        [Summary("A censor that deletes the message and also kicks the user.")]
        public async Task AddKickCensorAsync(string pattern, RegexOptions options = RegexOptions.None)
        {
            var censor = new KickCensor(pattern, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add note")]
        [Summary("A censor that deletes the message and does nothing to the user.")]
        public async Task AddNoteCensorAsync(string pattern, RegexOptions options = RegexOptions.None)
        {
            var censor = new NoteCensor(pattern, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("add mute")]
        [Summary("A censor that deletes the message and mutes the user.")]
        public async Task AddMuteCensorAsync(string pattern, RegexOptions options = RegexOptions.None,
            TimeSpan? length = null)
        {
            var censor = new MuteCensor(length, pattern, options);

            await AddCensor(censor);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        private async Task AddCensor(Censor censor)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.ModerationRules.Censors
                .Add(censor.WithModerator((IGuildUser) Context.User));

            await _db.SaveChangesAsync();
        }
    }
}