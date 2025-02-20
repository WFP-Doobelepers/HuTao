using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;

namespace HuTao.Data.Models.Discord;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract class Context(IDiscordClient client, IGuild guild, IMessageChannel channel, IUser user)
{
    /// <summary>
    ///     Gets the <see cref="T:Discord.IDiscordClient" /> that the context is executed with.
    /// </summary>
    public IDiscordClient Client { get; } = client;

    /// <summary>
    ///     Gets the <see cref="T:Discord.IGuild" /> that the context is executed in.
    /// </summary>
    public IGuild Guild { get; } = guild;

    /// <summary>
    ///     Gets the <see cref="T:Discord.IMessageChannel" /> that the context is executed in.
    /// </summary>
    public IMessageChannel Channel { get; } = channel;

    /// <summary>
    ///     Gets the <see cref="T:Discord.IUser" /> who executed the context.
    /// </summary>
    public IUser User { get; } = user;

    /// <inheritdoc cref="IDiscordInteraction.DeferAsync" />
    public virtual Task DeferAsync(bool ephemeral = false, RequestOptions? options = null) => Task.CompletedTask;

    /// <summary>Sends a message to the context.</summary>
    /// <param name="message">
    ///     Contents of the message; optional only if <paramref name="embed" /> is specified.
    /// </param>
    /// <param name="isTTS">Specifies if Discord should read this <paramref name="message" /> aloud using text-to-speech.</param>
    /// <param name="embed">An embed to be displayed alongside the <paramref name="message" />.</param>
    /// <param name="options">The request options for this <see langword="async" /> request.</param>
    /// <param name="allowedMentions">
    ///     Specifies if notifications are sent for mentioned users and roles in the <paramref name="message" />.
    ///     If <c>null</c>, all mentioned roles and users will be notified.
    /// </param>
    /// <param name="messageReference">The message references to be included. Used to reply to specific messages.</param>
    /// <param name="components">The message components to be included with this message. Used for interactions.</param>
    /// <param name="stickers">A collection of stickers to send with the file.</param>
    /// <param name="embeds">A array of <see cref="T:Discord.Embed" />s to send with this response. Max 10.</param>
    /// <param name="flags">
    ///     A message flag to be applied to the sent message, only
    ///     <see cref="F:Discord.MessageFlags.SuppressEmbeds" /> is permitted.
    /// </param>
    /// <param name="ephemeral">
    ///     <see langword="true" /> if the response should be hidden to everyone besides the invoker of the
    ///     command, otherwise <see langword="false" />.
    /// </param>
    public abstract Task ReplyAsync(
        string? message = null, bool isTTS = false, Embed? embed = null, RequestOptions? options = null,
        AllowedMentions? allowedMentions = null, MessageReference? messageReference = null,
        MessageComponent? components = null, ISticker[]? stickers = null, Embed[]? embeds = null,
        MessageFlags flags = MessageFlags.None, bool ephemeral = false);

    public static implicit operator Context(SocketCommandContext context) => new CommandContext(context);

    public static implicit operator Context(SocketInteractionContext context) => new InteractionContext(context);
}