namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class KickTrigger : WarningTrigger
    {
        public KickTrigger(uint amount) : base(amount) { }
    }
}