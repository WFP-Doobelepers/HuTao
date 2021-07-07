namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Warning : ReprimandAction, IWarning
    {
        protected Warning() { }

        public Warning(uint amount, ReprimandDetails details) : base(details) { Amount = amount; }

        public uint Amount { get; set; }
    }
}