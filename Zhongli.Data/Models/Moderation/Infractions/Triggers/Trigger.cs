using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public abstract class Trigger : IModerationAction, ITrigger
    {
        protected Trigger(uint amount, TriggerSource source, TriggerMode mode)
        {
            Amount = amount;
            Mode   = mode;
            Source = source;
        }

        public Guid Id { get; set; }

        public ModerationAction Action { get; set; }

        public TriggerMode Mode { get; set; }

        public TriggerSource Source { get; set; }

        public uint Amount { get; set; }
    }
}