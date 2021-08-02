using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public abstract class WarningTrigger : IModerationAction, ITrigger
    {
        protected WarningTrigger(uint amount, bool retroactive)
        {
            Amount      = amount;
            Retroactive = retroactive;
        }

        public Guid Id { get; set; }

        public virtual ModerationAction Action { get; set; }

        public uint Amount { get; set; }

        public bool Retroactive { get; set; }

        public bool IsTriggered(int amount)
            => Retroactive ? amount >= Amount : amount == Amount;
    }
}