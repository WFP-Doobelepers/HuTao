using System.Text.RegularExpressions;
using HuTao.Data.Models.Moderation.Infractions.Triggers;

namespace HuTao.Data.Models.Moderation.Infractions.Censors;

public interface ICensorOptions : ITrigger
{
    public bool Silent { get; }

    public RegexOptions Flags { get; }
}