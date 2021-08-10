namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public interface ITrigger
    {
        TriggerMode Mode { get; set; }

        TriggerSource Source { get; set; }

        uint Amount { get; set; }
    }
}