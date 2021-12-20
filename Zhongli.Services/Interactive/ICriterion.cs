using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Interactive;

public interface ICriterion<in T>
{
    bool Judge(SocketCommandContext context, T compare);
}

public class EnsureSourceChannelCriterion : ICriterion<SocketMessage>
{
    public bool Judge(SocketCommandContext context, SocketMessage compare)
        => context.Channel == compare.Channel;
}

public class EnsureSourceUserCriterion : ICriterion<SocketMessage>
{
    public bool Judge(SocketCommandContext context, SocketMessage compare)
        => context.User.Id == compare.Author.Id;
}