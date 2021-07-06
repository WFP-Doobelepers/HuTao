using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public abstract class WarningTrigger : IModerationAction, IWarning
    {
        protected WarningTrigger() { }

        protected WarningTrigger(uint amount) { Amount = amount; }

        public Guid Id { get; set; }

        public virtual ModerationAction Action { get; set; }

        public uint Amount { get; set; }

        public bool IsTriggered(GuildUserEntity user) => user.WarningCount >= Amount;

        public bool IsTriggered(int count) => count >= Amount;
    }
}