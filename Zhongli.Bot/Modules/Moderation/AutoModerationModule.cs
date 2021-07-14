using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Core.Preconditions;

namespace Zhongli.Bot.Modules.Moderation
{
    [RequireAuthorization(AuthorizationScope.Auto)]
    public class AutoModerationModule : ModuleBase
    {
        private readonly ZhongliContext _db;

        public AutoModerationModule(ZhongliContext db) { _db = db; }

        [Command("banAt")]
        public async Task BanAtAsync(uint amount, uint deleteDays = 0)
        {
            var rules = await GetModerationRules(Context.Guild.Id);

            if (rules.BanTrigger is not null) _db.Remove(rules.BanTrigger);
            rules.BanTrigger = new BanTrigger(amount, deleteDays).WithModerator((IGuildUser) Context.User);

            await _db.SaveChangesAsync();
        }

        [Command("kickAt")]
        public async Task KickAtAsync(uint amount)
        {
            var rules = await GetModerationRules(Context.Guild.Id);

            if (rules.KickTrigger is not null) _db.Remove(rules.KickTrigger);
            rules.KickTrigger = new KickTrigger(amount).WithModerator((IGuildUser) Context.User);

            await _db.SaveChangesAsync();
        }

        [Command("muteAt")]
        public async Task MuteAtAsync(uint amount, TimeSpan? length = null)
        {
            var rules = await GetModerationRules(Context.Guild.Id);
            var similar = rules.MuteTriggers.FirstOrDefault(m => m.Length == length);

            if (similar != null) _db.Remove(similar);
            rules.MuteTriggers.Add(new MuteTrigger(amount, length).WithModerator((IGuildUser) Context.User));

            await _db.SaveChangesAsync();
        }

        [Command("warnAt")]
        public async Task WarnAtAsync(uint amount)
        {
            var rules = await GetModerationRules(Context.Guild.Id);
            var similar = rules.NoticeTriggers.FirstOrDefault(m => m.Amount == amount);

            if (similar != null) _db.Remove(similar);
            rules.NoticeTriggers.Add(new NoticeTrigger(amount).WithModerator((IGuildUser) Context.User));

            await _db.SaveChangesAsync();
        }

        private async Task<AutoModerationRules> GetModerationRules(ulong guildId)
        {
            var guildEntity = await _db.Guilds.FindAsync(guildId);

            return guildEntity.AutoModerationRules;
        }
    }
}