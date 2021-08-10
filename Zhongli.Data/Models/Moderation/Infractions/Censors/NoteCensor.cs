namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class NoteCensor : Censor, INote
    {
        protected NoteCensor() { }

        public NoteCensor(string pattern, ICensorOptions? options) : base(pattern, options) { }
    }
}