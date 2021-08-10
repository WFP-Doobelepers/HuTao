namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class WarningTrigger : Trigger
    {
        public WarningTrigger(uint amount, TriggerSource source, TriggerMode mode) : base(amount, source, mode) { }
    }
}