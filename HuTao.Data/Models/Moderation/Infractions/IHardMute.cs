using Discord;
using Humanizer;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface IHardMute : IAction, ILength
{
    string IAction.Action => $"Hard Mute {Format.Bold(Length?.Humanize() ?? "indefinitely")}";

    string IAction.CleanAction => $"Hard Mute {Length?.Humanize() ?? "indefinitely"}";
}