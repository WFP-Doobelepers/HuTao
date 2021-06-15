using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Triggers
{
    public class WarningTrigger
    {
        public WarningTrigger() { }

        protected WarningTrigger(uint triggerAt) { TriggerAt = triggerAt; }

        public Guid Id { get; set; }

        public uint TriggerAt { get; set; }

        public bool IsTriggered(GuildUserEntity user) => user.WarningCount >= TriggerAt;

        public bool IsTriggered(int count) => count >= TriggerAt;
    }
}