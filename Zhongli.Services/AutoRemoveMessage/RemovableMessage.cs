using Discord;

namespace Zhongli.Services.AutoRemoveMessage;

public class RemovableMessage
{
    public IMessage Message { get; set; }

    public IUser[] Users { get; set; }
}