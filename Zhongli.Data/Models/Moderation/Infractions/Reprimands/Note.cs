namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Note : ReprimandAction, INote
    {
        protected Note() { }

        public Note(ReprimandDetails details) : base(details) { }
    }
}