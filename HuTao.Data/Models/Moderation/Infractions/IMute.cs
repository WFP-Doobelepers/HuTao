using Discord;
using Humanizer;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface IMute : IAction, ILength
{
    string IAction.Action => $"Mute {Format.Bold(Length?.Humanize() ?? "indefinitely")}";

    string IAction.CleanAction => $"Mute {Length?.Humanize() ?? "indefinitely"}";
}