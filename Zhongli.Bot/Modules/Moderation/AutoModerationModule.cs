﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation;
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
        public async Task BanAtAsync(uint triggerAt, uint deleteDays = 0)
        {
            var rules = await GetModerationRules(Context.Guild.Id);

            if (rules.BanTrigger is not null) _db.Remove(rules.BanTrigger);
            rules.BanTrigger = new BanTrigger(triggerAt, deleteDays);

            await _db.SaveChangesAsync();
        }

        [Command("kickAt")]
        public async Task KickAtAsync(uint triggerAt)
        {
            var rules = await GetModerationRules(Context.Guild.Id);

            if (rules.KickTrigger is not null) _db.Remove(rules.KickTrigger);
            rules.KickTrigger = new KickTrigger(triggerAt);

            await _db.SaveChangesAsync();
        }

        [Command("muteAt")]
        public async Task MuteAtAsync(uint triggerAt, TimeSpan? length = null)
        {
            var rules = await GetModerationRules(Context.Guild.Id);
            var similar = rules.MuteTriggers.FirstOrDefault(m => m.Length == length);

            if (similar != null) _db.Remove(similar);
            rules.MuteTriggers.Add(new MuteTrigger(triggerAt, length));

            await _db.SaveChangesAsync();
        }

        private async Task<AutoModerationRules> GetModerationRules(ulong guildId)
        {
            var guildEntity = await _db.Guilds.FindAsync(guildId);

            return guildEntity.AutoModerationRules;
        }
    }
}