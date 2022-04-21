using System.ComponentModel;

namespace HuTao.Data.Models.Moderation.Infractions.Triggers;

public enum TriggerMode
{
    [Description("Trigger exactly at the amount.")]
    Exact,

    [Description("Trigger on equal or greater than the amount.")]
    Retroactive,

    [Description("Trigger on a multiple of the amount.")]
    Multiple
}