using Discord;
using Discord.Commands;

namespace Zhongli.Data.Models.Discord;

/// <inheritdoc cref="ICommandContext" />
public class CommandContext : Context, ICommandContext
{
    public CommandContext(ICommandContext context)
        : base(context.Client, context.Guild, context.Channel, context.User)
    {
        Message = context.Message;
    }

    /// <inheritdoc />
    public IUserMessage Message { get; }
}