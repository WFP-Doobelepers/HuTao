namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class WarningAction(uint count) : ReprimandAction, IWarning
{
    public uint Count { get; set; } = count;
}