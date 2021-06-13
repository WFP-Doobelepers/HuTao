using System;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Reprimands;

namespace Zhongli.Data.Models.Moderation
{
    public class WarningTrigger
    {
        public WarningTrigger() { }

        protected WarningTrigger(uint triggerAt, Reprimand reprimand)
        {
            TriggerAt = triggerAt;
            Reprimand = reprimand;
        }

        public Guid Id { get; set; }

        public Reprimand Reprimand { get; set; }

        public uint TriggerAt { get; set; }

        public bool IsTriggered(GuildUserEntity user) => user.WarningCount >= TriggerAt;
    }
}