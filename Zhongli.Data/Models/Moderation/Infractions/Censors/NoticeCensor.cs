namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class NoticeCensor : Censor, INotice
    {
        protected NoticeCensor() { }

        public NoticeCensor(string pattern, ICensorOptions? options) : base(pattern, options) { }
    }
}