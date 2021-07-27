using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class WarnCensor : Censor
    {
        protected WarnCensor() { }

        public WarnCensor(uint amount, string pattern, RegexOptions options = RegexOptions.None) : base(pattern,
            options)
        {
            Amount = amount;
        }

        public uint Amount { get; set; }
    }
}