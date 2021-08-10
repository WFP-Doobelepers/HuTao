namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class NoticeTrigger : Trigger
    {
        public NoticeTrigger(uint amount, TriggerMode mode, TriggerSource source) : base(amount, source, mode) { }
    }
}