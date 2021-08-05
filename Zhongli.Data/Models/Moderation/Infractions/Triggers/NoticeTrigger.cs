using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class NoticeTrigger : IModerationAction, ITrigger
    {
        public NoticeTrigger(uint amount, TriggerMode mode)
        {
            Amount = amount;
            Mode   = mode;
        }

        public Guid Id { get; set; }

        public virtual ModerationAction Action { get; set; }

        public uint Amount { get; set; }

        public TriggerMode Mode { get; set; }
    }
}