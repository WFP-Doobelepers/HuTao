namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class KickTrigger : WarningTrigger
    {
        public KickTrigger(uint amount, TriggerMode mode) : base(amount, mode) { }
    }
}