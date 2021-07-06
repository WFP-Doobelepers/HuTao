namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class BanTrigger : WarningTrigger, IBan
    {
        public BanTrigger(uint amount, uint deleteDays = 0)
            : base(amount)
        {
            DeleteDays = deleteDays;
        }

        public uint DeleteDays { get; set; }
    }
}