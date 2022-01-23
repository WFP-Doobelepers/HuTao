namespace Zhongli.Data.Models.Discord.Message.Linking;

public interface IMessageTemplateOptions
{
    public bool AllowMentions { get; set; }

    public bool IsLive { get; set; }

    public bool ReplaceTimestamps { get; set; }
}