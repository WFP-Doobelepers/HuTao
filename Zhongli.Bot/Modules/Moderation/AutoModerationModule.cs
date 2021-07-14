using System;
using System.Collections.Generic;
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
        public async Task BanAtAsync(uint amount, bool retroactive = false, uint deleteDays = 0)
        {
            var rules = await GetModerationRules(Context.Guild.Id);
            TryRemoveTrigger(rules.WarningTriggers, amount);

            var trigger = new BanTrigger(amount, retroactive, deleteDays).WithModerator((IGuildUser) Context.User);
            rules.WarningTriggers.Add(trigger);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("kickAt")]
        public async Task KickAtAsync(uint amount, bool retroactive = false)
        {
            var rules = await GetModerationRules(Context.Guild.Id);
            TryRemoveTrigger(rules.WarningTriggers, amount);

            var trigger = new KickTrigger(amount, retroactive).WithModerator((IGuildUser) Context.User);
            rules.WarningTriggers.Add(trigger);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("muteAt")]
        public async Task MuteAtAsync(uint amount, bool retroactive = false, TimeSpan? length = null)
        {
            var rules = await GetModerationRules(Context.Guild.Id);
            TryRemoveTrigger(rules.WarningTriggers, amount);

            var trigger = new MuteTrigger(amount, retroactive, length).WithModerator((IGuildUser) Context.User);
            rules.WarningTriggers.Add(trigger);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("warnAt")]
        public async Task WarnAtAsync(uint amount, bool retroactive = false)
        {
            var rules = await GetModerationRules(Context.Guild.Id);
            TryRemoveTrigger(rules.NoticeTriggers, amount);

            var trigger = new NoticeTrigger(amount, retroactive).WithModerator((IGuildUser) Context.User);
            rules.NoticeTriggers.Add(trigger);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        private void TryRemoveTrigger(IEnumerable<ITrigger> triggers, uint amount)
        {
            var existing = triggers.FirstOrDefault(w => w.Amount == amount);
            if (existing is not null) _db.Remove((object) existing);
        }

        private async Task<AutoModerationRules> GetModerationRules(ulong guildId)
        {
            var guildEntity = await _db.Guilds.FindAsync(guildId);

            return guildEntity.AutoModerationRules;
        }
    }
}