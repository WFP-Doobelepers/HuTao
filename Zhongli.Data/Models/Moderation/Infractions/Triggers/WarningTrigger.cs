using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public abstract class WarningTrigger : IModerationAction, IWarning, ITrigger
    {
        protected WarningTrigger() { }

        protected WarningTrigger(uint amount, bool retroactive = false)
        {
            Amount      = amount;
            Retroactive = retroactive;
        }

        public Guid Id { get; set; }

        public virtual ModerationAction Action { get; set; }

        public bool Retroactive { get; set; }

        public bool IsTriggered(uint amount)
            => Retroactive ? amount >= Amount : amount == Amount;

        public uint Amount { get; set; }
    }
}