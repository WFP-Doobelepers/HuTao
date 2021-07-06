namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Ban : ReprimandAction, IBan
    {
        protected Ban() { }

        public Ban(ReprimandDetails details, uint deleteDays) : base(details) { DeleteDays = deleteDays; }

        public uint DeleteDays { get; set; }
    }
}