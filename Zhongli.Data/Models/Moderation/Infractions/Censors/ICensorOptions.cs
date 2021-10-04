using System.Text.RegularExpressions;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public interface ICensorOptions : ITrigger
    {
        public bool Silent { get; set; }

        public RegexOptions Flags { get; set; }
    }
}