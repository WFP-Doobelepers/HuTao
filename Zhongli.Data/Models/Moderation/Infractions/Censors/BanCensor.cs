using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class BanCensor : Censor, IBan
    {
        protected BanCensor() { }

        public BanCensor(uint deleteDays, string pattern, RegexOptions options = RegexOptions.None) : base(pattern,
            options)
        {
            DeleteDays = deleteDays;
        }

        public uint DeleteDays { get; set; }
    }
}