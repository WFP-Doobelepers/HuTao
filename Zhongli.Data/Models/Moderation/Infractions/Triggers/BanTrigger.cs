namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class BanTrigger : WarningTrigger, IBan
    {
        public BanTrigger(uint amount, bool retroactive, uint deleteDays = 0)
            : base(amount, retroactive)
        {
            DeleteDays = deleteDays;
        }

        public uint DeleteDays { get; set; }
    }
}