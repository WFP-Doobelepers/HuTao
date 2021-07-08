using System;
using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class MuteCensor : Censor, IMute
    {
        protected MuteCensor() { }

        public MuteCensor(TimeSpan? length, string pattern, RegexOptions options = RegexOptions.None) : base(pattern,
            options)
        {
            Length = length;
        }

        public TimeSpan? Length { get; set; }
    }
}