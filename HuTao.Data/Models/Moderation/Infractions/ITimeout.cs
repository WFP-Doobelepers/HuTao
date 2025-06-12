using Discord;
using Humanizer;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface ITimeout: IAction, ILength
{
    string IAction.Action => $"Timeout {Format.Bold(Length?.Humanize() ?? "indefinitely")}";

    string IAction.CleanAction => $"Timeout {Length?.Humanize() ?? "indefinitely"}";
}