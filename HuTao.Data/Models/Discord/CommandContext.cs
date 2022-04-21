using Discord;
using Discord.Commands;

namespace HuTao.Data.Models.Discord;

/// <inheritdoc cref="ICommandContext" />
public class CommandContext : Context, ICommandContext
{
    public CommandContext(ICommandContext context)
        : base(context.Client, context.Guild, context.Channel, context.User)
    {
        Message = context.Message;
    }

    public IUserMessage Message { get; }
}