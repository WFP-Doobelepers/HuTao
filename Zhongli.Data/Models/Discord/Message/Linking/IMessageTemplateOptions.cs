namespace Zhongli.Data.Models.Discord.Message.Linking;

public interface IMessageTemplateOptions
{
    public bool AllowMentions { get; }

    public bool IsLive { get; }

    public bool ReplaceTimestamps { get; }

    public bool SuppressEmbeds { get; }
}