using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace HuTao.Data.Models.Discord;

/// <inheritdoc cref="ICommandContext" />
public class CommandContext(ICommandContext context)
    : Context(context.Client, context.Guild, context.Channel, context.User), ICommandContext
{
    public IUserMessage Message { get; } = context.Message;

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