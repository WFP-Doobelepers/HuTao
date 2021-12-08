using Discord;

namespace Zhongli.Data.Models.Moderation.Infractions.Actions;

public class WarningAction : ReprimandAction, IWarning
{
    public WarningAction(uint count) { Count = count; }

    public override string Action => $"Warn {Format.Bold(Count + " times")}";

    public uint Count { get; set; }
}