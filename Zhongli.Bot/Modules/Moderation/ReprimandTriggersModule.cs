using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class ReprimandTriggersModule : ModuleBase
    {
        private readonly ZhongliContext _db;

        public ReprimandTriggersModule(ZhongliContext db) { _db = db; }

        [Command("banAt")]
        public async Task BanAtAsync(uint amount, TriggerSource source,
            TriggerMode mode = TriggerMode.Default, uint deleteDays = 0,
            TimeSpan? length = null)
        {
            var trigger = new BanTrigger(amount, source, mode, deleteDays, length);
            await TryAddTriggerAsync(trigger);
        }

        [Command("kickAt")]
        public async Task KickAtAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Default)
        {
            var trigger = new KickTrigger(amount, source, mode);
            await TryAddTriggerAsync(trigger);
        }

        [Command("muteAt")]
        public async Task MuteAtAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Default,
            TimeSpan? length = null)
        {
            var trigger = new MuteTrigger(amount, mode, source, length);
            await TryAddTriggerAsync(trigger);
        }

        [Command("warnAt")]
        public async Task WarnAtAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Default)
        {
            var trigger = new WarningTrigger(amount, source, mode);
            await TryAddTriggerAsync(trigger);
        }

        [Command("noticeAt")]
        public async Task NoticeAtAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Default)
        {
            var trigger = new NoticeTrigger(amount, source, mode);
            await TryAddTriggerAsync(trigger);
        }

        private async Task TryAddTriggerAsync(Trigger trigger)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var rules = guild.ModerationRules;

            var existing = rules.Triggers
                .FirstOrDefault(t => t.Amount == trigger.Amount && t.Source == trigger.Source);
            if (existing is not null) _db.Remove(existing);

            rules.Triggers.Add(trigger.WithModerator(Context));
            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }
}