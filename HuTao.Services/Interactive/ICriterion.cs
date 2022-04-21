using Discord;
using Discord.Commands;

namespace HuTao.Services.Interactive;

public interface ICriterion<in T>
{
    bool Judge(SocketCommandContext context, T compare);
}

public class EnsureSourceChannelCriterion : ICriterion<IMessage>
{
    public bool Judge(SocketCommandContext context, IMessage compare)
        => context.Channel == compare.Channel;
}

public class EnsureSourceUserCriterion : ICriterion<IMessage>
{
    public bool Judge(SocketCommandContext context, IMessage compare)
        => context.User.Id == compare.Author.Id;
}