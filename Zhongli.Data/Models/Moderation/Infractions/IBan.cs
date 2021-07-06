namespace Zhongli.Data.Models.Moderation.Infractions
{
    public interface IBan
    {
        uint DeleteDays { get; set; }
    }
}