using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

namespace Zhongli.Services.Interactive;

public abstract class Context
{
    protected Context(IDiscordClient client, IGuild guild, IMessageChannel channel, IUser user)
    {
        Client  = client;
        Guild   = guild;
        Channel = channel;
        User    = user;
    }

    public IDiscordClient Client { get; }

    public IGuild Guild { get; }

    public IMessageChannel Channel { get; }

    public IUser User { get; }
}

public class CommandContext : Context
{
    public CommandContext(ICommandContext context)
        : base(context.Client, context.Guild, context.Channel, context.User)
    {
        Message = context.Message;
    }

    public IUserMessage Message { get; }
}

public class InteractionContext : Context
{
    public InteractionContext(SocketInteractionContext context)
        : base(context.Client, context.Guild, context.Channel, context.User)
    {
        Interaction = context.Interaction;
    }

    public SocketInteraction Interaction { get; }
}