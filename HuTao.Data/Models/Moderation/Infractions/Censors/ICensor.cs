using System.Text.RegularExpressions;

namespace HuTao.Data.Models.Moderation.Infractions.Censors;

public interface ICensor
{
    RegexOptions Options { get; set; }

    string Pattern { get; set; }
}