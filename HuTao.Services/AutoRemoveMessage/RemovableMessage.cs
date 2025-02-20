using Discord;

namespace HuTao.Services.AutoRemoveMessage;

public class RemovableMessage(IMessage message, IUser[] users)
{
    public IMessage Message { get; set; } = message;

    public IUser[] Users { get; set; } = users;
}