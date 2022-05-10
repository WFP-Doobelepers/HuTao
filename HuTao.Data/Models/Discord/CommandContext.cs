using System.Threading.Tasks;
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

    public override async Task ReplyAsync(
        string? message = null, bool isTTS = false, Embed? embed = null, RequestOptions? options = null,
        AllowedMentions? allowedMentions = null, MessageReference? messageReference = null,
        MessageComponent? components = null, ISticker[]? stickers = null, Embed[]? embeds = null,
        MessageFlags flags = MessageFlags.None, bool ephemeral = false)
        => await Channel.SendMessageAsync(
                message, isTTS, embed, options, allowedMentions ?? AllowedMentions.None,
                messageReference ?? Message.Reference ?? new MessageReference(Message.Id),
                components, stickers, embeds, flags)
            .ConfigureAwait(false);
}