namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public class Note : Reprimand, INote
    {
        protected Note() { }

        public Note(ReprimandDetails details) : base(details) { }
    }
}