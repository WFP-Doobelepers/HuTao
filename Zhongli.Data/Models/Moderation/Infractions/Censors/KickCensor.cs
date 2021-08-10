using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class KickCensor : Censor, IKick
    {
        protected KickCensor() { }

        public KickCensor(string pattern, RegexOptions options = RegexOptions.None) : base(pattern, options) { }
    }
}