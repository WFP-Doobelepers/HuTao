namespace Zhongli.Data.Models.Moderation.Infractions.Actions
{
    public class WarningAction : ReprimandAction, IWarning
    {
        public WarningAction(uint count) { Count = count; }

        public uint Count { get; set; }
    }
}