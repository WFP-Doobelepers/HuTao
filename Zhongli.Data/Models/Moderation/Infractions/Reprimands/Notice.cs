namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Notice : ReprimandAction
    {
        protected Notice() { }

        public Notice(ReprimandDetails details) : base(details) { }
    }
}