using System.Text.RegularExpressions;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public interface ICensorOptions
    {
        public RegexOptions Options { get; set; }

        public TriggerMode Mode { get; set; }

        public uint Amount { get; set; }
    }
}