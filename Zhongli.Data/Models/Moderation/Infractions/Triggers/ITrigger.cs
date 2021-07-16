namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public interface ITrigger
    {
        bool Retroactive { get; set; }

        uint Amount { get; set; }

        bool IsTriggered(int amount);
    }
}