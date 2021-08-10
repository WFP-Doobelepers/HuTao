using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class NoticeCensor : Censor
    {
        protected NoticeCensor() { }

        public NoticeCensor(string pattern, RegexOptions options = RegexOptions.None) : base(pattern, options) { }
    }
}