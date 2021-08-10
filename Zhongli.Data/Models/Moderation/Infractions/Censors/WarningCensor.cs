using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class WarningCensor : Censor, IWarning
    {
        protected WarningCensor() { }

        public WarningCensor(uint amount, string pattern, RegexOptions options = RegexOptions.None) : base(pattern,
            options)
        {
            Count = amount;
        }

        public uint Count { get; set; }
    }
}