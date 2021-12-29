using Discord;

namespace Zhongli.Services.AutoRemoveMessage;

public class RemovableMessage
{
    public RemovableMessage(IMessage message, IUser[] users)
    {
        Message = message;
        Users   = users;
    }

    public IMessage Message { get; set; }

    public IUser[] Users { get; set; }
}