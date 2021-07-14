using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public abstract class WarningTrigger : IModerationAction, IWarning
    {
        protected WarningTrigger() { }

        protected WarningTrigger(uint amount) { Amount = amount; }

        public Guid Id { get; set; }

        public Guid AutoModerationRulesId { get; set; }

        public virtual ModerationAction Action { get; set; }

        public uint Amount { get; set; }

        public virtual bool IsTriggered(GuildUserEntity user) => user.WarningCount >= Amount;
    }
}