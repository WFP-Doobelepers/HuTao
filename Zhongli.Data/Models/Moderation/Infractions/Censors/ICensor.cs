using System.Text.RegularExpressions;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors;

public interface ICensor
{
    RegexOptions Options { get; set; }

    string Pattern { get; set; }
}