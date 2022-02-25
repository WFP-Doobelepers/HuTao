using Discord;

namespace Zhongli.Data.Models.Discord.Message.Linking;

public interface ILinkedButtonOptions : IRoleTemplateOptions, IMessageTemplateOptions
{
    public bool Ephemeral { get; }

    public bool IsDisabled { get; }

    public ButtonStyle Style { get; }

    public IEmote? Emote { get; }

    public IMessage? Message { get; }

    public int Row { get; set; }

    public MessageTemplate? MessageTemplate
        => Message is null ? null : new MessageTemplate(Message, this);

    public string? Label { get; }

    public string? Url { get; }
}