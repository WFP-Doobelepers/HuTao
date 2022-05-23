namespace HuTao.Data.Models.TimeTracking;

public class MessageTimeTracking : TimeTracking
{
    public string JumpUrl => $"https://discord.com/channels/{GuildId}/{ChannelId}/{MessageId}";

    public ulong ChannelId { get; set; }

    public ulong MessageId { get; set; }
}