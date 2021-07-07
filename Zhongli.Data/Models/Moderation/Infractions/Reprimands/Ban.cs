namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Ban : ReprimandAction, IBan
    {
        protected Ban() { }

        public Ban(uint deleteDays, ReprimandDetails details) : base(details) { DeleteDays = deleteDays; }

        public uint DeleteDays { get; set; }
    }
}