using Discord;
using Humanizer;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface IBan : IAction, ILength
{
    public uint DeleteDays { get; set; }

    string IAction.Action
        => $"Ban {Format.Bold(Length?.Humanize() ?? "indefinitely")} and delete {Format.Bold(DeleteDays + " days")} of messages";

    string IAction.CleanAction
        => $"Ban {Length?.Humanize() ?? "indefinitely"} and delete {DeleteDays + " days"} of messages";
}