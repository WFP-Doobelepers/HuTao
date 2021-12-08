namespace Zhongli.Data.Models.Moderation.Infractions;

public interface IBan : ILength
{
    uint DeleteDays { get; set; }
}