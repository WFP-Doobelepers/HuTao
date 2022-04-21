using Discord;

namespace Zhongli.Data.Models.Moderation.Infractions;

public interface IWarning : IAction
{
    public uint Count { get; set; }

    string IAction.Action => $"Warn {Format.Bold(Count + " times")}";

    string IAction.CleanAction => $"Warn {Count + " times"}";
}