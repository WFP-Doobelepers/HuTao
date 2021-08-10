namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class KickTrigger : Trigger, IKick
    {
        public KickTrigger(uint amount, TriggerSource source, TriggerMode mode) : base(amount, source, mode) { }
    }
}