namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Kick : ReprimandAction
    {
        protected Kick() { }

        public Kick(ReprimandDetails details) : base(details) { }
    }
}