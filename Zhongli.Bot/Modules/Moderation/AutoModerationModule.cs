using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Triggers;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    public class AutoModerationModule : ModuleBase
    {
        private readonly ZhongliContext _db;

        public AutoModerationModule(ZhongliContext db)
        {
            _db = db;
        }

        [Command("muteAt")]
        [RequireAuthorization(AuthorizationScope.Auto)]
        public async Task MuteAtAsync(uint triggerAt, TimeSpan? length = null)
        {
            var rules = await AutoConfigureGuild(Context.Guild.Id);
            var similar = rules.MuteTriggers.FirstOrDefault(m => m.Length == length);

            if (similar != null) _db.Remove(similar);
            rules.MuteTriggers.Add(new MuteTrigger(triggerAt, length));

            await _db.SaveChangesAsync();
        }

        [Command("banAt")]
        [RequireAuthorization(AuthorizationScope.Auto)]
        public async Task BanAtAsync(uint triggerAt, uint deleteDays = 0)
        {
            var rules = await AutoConfigureGuild(Context.Guild.Id);

            if (rules.BanTrigger is not null) _db.Remove(rules.BanTrigger);
            rules.BanTrigger = new BanTrigger(triggerAt, deleteDays);

            await _db.SaveChangesAsync();
        }

        [Command("kickAt")]
        [RequireAuthorization(AuthorizationScope.Auto)]
        public async Task KickAtAsync(uint triggerAt)
        {
            var rules = await AutoConfigureGuild(Context.Guild.Id);

            if (rules.KickTrigger is not null) _db.Remove(rules.KickTrigger);
            rules.KickTrigger = new KickTrigger(triggerAt);

            await _db.SaveChangesAsync();
        }

        private async Task<AutoModerationRules> AutoConfigureGuild(ulong guildId)
        {
            var guildEntity = await _db.Guilds.FindAsync(guildId);

            if (guildEntity.AutoModerationRules is not null)
                return guildEntity.AutoModerationRules;

            guildEntity.AutoModerationRules = _db.Add(new AutoModerationRules
            {
                AntiSpamRules = new AntiSpamRules()
            }).Entity;

            await _db.SaveChangesAsync();

            return guildEntity.AutoModerationRules!;
        }
    }
}