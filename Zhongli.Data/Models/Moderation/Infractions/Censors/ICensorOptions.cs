using System.Text.RegularExpressions;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public interface ICensorOptions
    {
        public RegexOptions Flags { get; set; }

        public TriggerMode TriggerMode { get; set; }

        public uint? TriggerAt { get; set; }
    }
}