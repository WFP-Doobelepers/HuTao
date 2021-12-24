using Discord;
using Discord.Commands;

namespace Zhongli.Services.Interactive;

public class Context : ICommandContext, IInteractionContext
{
    private Context(IDiscordClient client, IGuild guild, IMessageChannel channel, IUser user)
    {
        Client  = client;
        Guild   = guild;
        Channel = channel;
        User    = user;
    }

    public Context(ICommandContext context)
        : this(context.Client, context.Guild, context.Channel, context.User)
    {
        Message = context.Message;
    }

    public Context(IInteractionContext context)
        : this(context.Client, context.Guild, context.Channel, context.User)
    {
        Interaction = context.Interaction;
    }

    public IDiscordClient Client { get; set; }

    public IGuild Guild { get; set; }

    public IMessageChannel Channel { get; set; }

    public IUser User { get; set; }

    public IUserMessage? Message { get; set; }

    public IDiscordInteraction? Interaction { get; set; }
}