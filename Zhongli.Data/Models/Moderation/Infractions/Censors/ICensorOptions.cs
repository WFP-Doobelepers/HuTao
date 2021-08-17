using System.Text.RegularExpressions;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public interface ICensorOptions : ITrigger
    {
        public RegexOptions Flags { get; set; }
    }
}