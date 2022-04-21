using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using MediatR;

namespace HuTao.Services.AutoRemoveMessage;

/// <summary>
///     Defines a service used to track removable messages.
/// </summary>
public interface IRemovableMessageService
{
    /// <summary>
    ///     Registers a removable message with the service and adds an indicator for this to the provided embed.
    /// </summary>
    /// <param name="user">The user who can remove the message.</param>
    /// <param name="embeds">The embeds to operate on</param>
    /// <param name="callback">
    ///     A callback that returns the <see cref="IUserMessage" /> to register as removable. The modified
    ///     embed is provided with this callback.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    /// If the provided
    /// <paramref name="callback" />
    /// is null.
    /// <returns>
    ///     A <see cref="Task" /> that will complete when the operation completes.
    /// </returns>
    Task RegisterRemovableMessageAsync(IUser user,
        IReadOnlyCollection<EmbedBuilder> embeds,
        Func<IReadOnlyCollection<EmbedBuilder>, Task<IUserMessage>> callback);

    /// <summary>
    ///     Registers a removable message with the service and adds an indicator for this to the provided embed.
    /// </summary>
    /// <param name="users">The users who can remove the message.</param>
    /// <param name="embeds">The embeds to operate on</param>
    /// <param name="callback">
    ///     A callback that returns the <see cref="IUserMessage" /> to register as removable. The modified
    ///     embed is provided with this callback.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    /// If the provided
    /// <paramref name="callback" />
    /// is null.
    /// <returns>
    ///     A <see cref="Task" /> that will complete when the operation completes.
    /// </returns>
    Task RegisterRemovableMessageAsync(IUser[] users,
        IReadOnlyCollection<EmbedBuilder> embeds,
        Func<IReadOnlyCollection<EmbedBuilder>, Task<IUserMessage>> callback);

    /// <summary>
    ///     Unregisters a removable message from the service.
    /// </summary>
    /// <param name="message">The removable message.</param>
    void UnregisterRemovableMessage(IMessage message);
}

/// <inheritdoc />
internal class RemovableMessageService : IRemovableMessageService
{
    private const string FooterReactMessage = "React with ❌ to remove this embed.";
    private readonly IMediator _mediator;

    public RemovableMessageService(IMediator mediator) { _mediator = mediator; }

    /// <inheritdoc />
    public Task RegisterRemovableMessageAsync(IUser user, IReadOnlyCollection<EmbedBuilder> embeds,
        Func<IReadOnlyCollection<EmbedBuilder>, Task<IUserMessage>> callback)
        => RegisterRemovableMessageAsync(new[] { user }, embeds, callback);

    /// <inheritdoc />
    public async Task RegisterRemovableMessageAsync(IUser[] users,
        IReadOnlyCollection<EmbedBuilder> embeds,
        Func<IReadOnlyCollection<EmbedBuilder>, Task<IUserMessage>> callback)
    {
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        ModifyFirstEmbed(embeds);

        var msg = await callback(embeds);
        await _mediator.Publish(new RemovableMessageSentNotification(msg, users));
    }

    /// <inheritdoc />
    public void UnregisterRemovableMessage(IMessage message)
        => _mediator.Publish(new RemovableMessageRemovedNotification(message));

    private static void ModifyFirstEmbed(IEnumerable<EmbedBuilder> embeds)
    {
        var embed = embeds.FirstOrDefault();
        if (string.IsNullOrEmpty(embed?.Footer?.Text))
            embed?.WithFooter(FooterReactMessage);
        else if (!embed.Footer.Text.Contains(FooterReactMessage))
            embed.Footer.Text += $" | {FooterReactMessage}";
    }
}