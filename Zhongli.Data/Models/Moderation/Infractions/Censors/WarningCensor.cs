using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class WarningCensor : Censor
    {
        protected WarningCensor() { }

        public WarningCensor(uint amount, string pattern, RegexOptions options = RegexOptions.None) : base(pattern,
            options)
        {
            Amount = amount;
        }

        public uint Amount { get; set; }
    }
}