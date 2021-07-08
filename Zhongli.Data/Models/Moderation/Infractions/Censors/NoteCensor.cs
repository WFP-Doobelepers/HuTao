using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class NoteCensor : Censor
    {
        protected NoteCensor() { }

        public NoteCensor(string pattern, RegexOptions options = RegexOptions.None) : base(pattern, options) { }
    }
}