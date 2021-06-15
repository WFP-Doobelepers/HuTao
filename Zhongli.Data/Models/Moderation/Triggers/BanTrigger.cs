namespace Zhongli.Data.Models.Moderation.Triggers
{
    public class BanTrigger : WarningTrigger
    {
        public BanTrigger(uint triggerAt, uint deleteDays = 0)
            : base(triggerAt)
        {
            DeleteDays = deleteDays;
        }

        public uint DeleteDays { get; set; }
    }
}