using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public abstract class Trigger : ITrigger, IModerationAction
    {
        protected Trigger() { }

        protected Trigger(ITrigger? options)
        {
            Mode   = options?.Mode ?? TriggerMode.Exact;
            Amount = options?.Amount ?? 1;
        }

        public Guid Id { get; set; }

        public virtual ModerationAction Action { get; set; }

        public TriggerMode Mode { get; set; }

        public uint Amount { get; set; }
    }
}