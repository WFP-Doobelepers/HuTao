namespace HuTao.Data.Models.Moderation.Infractions.Triggers;

public interface ITrigger
{
    TriggerMode Mode { get; set; }

    uint Amount { get; set; }
}