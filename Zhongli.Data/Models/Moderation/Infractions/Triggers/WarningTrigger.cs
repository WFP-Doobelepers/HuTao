namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class WarningTrigger : Trigger, IWarning
    {
        public WarningTrigger(uint amount, TriggerSource source, TriggerMode mode, uint count) : base(amount, source, mode) { Count = count; }

        public uint Count { get; set; }
    }
}