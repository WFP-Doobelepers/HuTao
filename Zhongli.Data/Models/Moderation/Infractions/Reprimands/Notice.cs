namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Notice : Warning
    {
        protected Notice() { }

        public Notice(uint amount, ReprimandDetails details) : base(amount, details) { }
    }
}