namespace HuTao.Data.Models.Moderation.Infractions.Triggers;

public interface ITrigger
{
    public ModerationCategory? Category { get; set; }

    public TriggerMode Mode { get; set; }

    public uint Amount { get; set; }
}