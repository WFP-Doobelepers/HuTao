using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
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
            uint deleteDays = 0, TimeSpan? length = null,
            TriggerMode mode = TriggerMode.Exact)
        {
            var action = new BanAction(deleteDays, length);
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("kickAt")]
        public async Task KickAtAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Exact)
        {
            var action = new KickAction();
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("muteAt")]
        public async Task MuteAtAsync(uint amount, TriggerSource source, TimeSpan? length = null,
            TriggerMode mode = TriggerMode.Exact)
        {
            var action = new MuteAction(length);
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("noticeAt")]
        public async Task NoticeAtAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Exact)
        {
            var action = new NoticeAction();
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        [Command("warnAt")]
        public async Task WarnAtAsync(uint amount, TriggerSource source,
            uint count = 1, TriggerMode mode = TriggerMode.Exact)
        {
            var action = new WarningAction(count);
            await TryAddTriggerAsync(action, amount, source, mode);
        }

        private async Task TryAddTriggerAsync(ReprimandAction action, uint amount, TriggerSource source,
            TriggerMode mode)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var rules = guild.ModerationRules;

            var options = new TriggerOptions(amount, source, mode);
            var trigger = new ReprimandTrigger(options, options.Source, action);

            var existing = rules.Triggers
                .OfType<ReprimandTrigger>()
                .FirstOrDefault(t => t.Source == options.Source && t.Amount == trigger.Amount);
            if (existing is not null) _db.Remove(existing);

            rules.Triggers.Add(trigger.WithModerator(Context));
            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        private class TriggerOptions : ITrigger
        {
            public TriggerOptions(uint amount, TriggerSource source, TriggerMode mode)
            {
                Mode   = mode;
                Amount = amount;
                Source = source;
            }

            public TriggerSource Source { get; }

            public TriggerMode Mode { get; set; }

            public uint Amount { get; set; }
        }
    }
}