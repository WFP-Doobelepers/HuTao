using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class NoticeTrigger : IModerationAction, ICountable, ITrigger
    {
        protected NoticeTrigger() { }

        public NoticeTrigger(uint amount, bool retroactive)
        {
            Amount      = amount;
            Retroactive = retroactive;
        }

        public Guid Id { get; set; }

        public uint Amount { get; set; }

        public virtual ModerationAction Action { get; set; }

        public bool Retroactive { get; set; }

        public bool IsTriggered(int amount)
            => Retroactive ? amount >= Amount : amount == Amount;
    }
}