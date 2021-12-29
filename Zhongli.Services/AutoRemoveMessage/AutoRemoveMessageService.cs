using System;
using System.Threading.Tasks;
using Discord;
using MediatR;

namespace Zhongli.Services.AutoRemoveMessage;

/// <summary>
///     Defines a service used to track removable messages.
/// </summary>
public interface IAutoRemoveMessageService
{
    /// <summary>
    ///     Registers a removable message with the service and adds an indicator for this to the provided embed.
    /// </summary>
    /// <param name="user">The user who can remove the message.</param>
    /// <param name="embed">The embed to operate on</param>
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
    Task RegisterRemovableMessageAsync(IUser user, EmbedBuilder embed,
        Func<EmbedBuilder, Task<IUserMessage>> callback);

    /// <summary>
    ///     Registers a removable message with the service and adds an indicator for this to the provided embed.
    /// </summary>
    /// <param name="users">The users who can remove the message.</param>
    /// <param name="embed">The embed to operate on</param>
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
    Task RegisterRemovableMessageAsync(IUser[] users, EmbedBuilder embed,
        Func<EmbedBuilder, Task<IUserMessage>> callback);

    /// <summary>
    ///     Unregisters a removable message from the service.
    /// </summary>
    /// <param name="message">The removable message.</param>
    void UnregisterRemovableMessage(IMessage message);
}

/// <inheritdoc />
internal class AutoRemoveMessageService : IAutoRemoveMessageService
{
    private const string FooterReactMessage = "React with ❌ to remove this embed.";
    private readonly IMediator _messageDispatcher;

    public AutoRemoveMessageService(IMediator messageDispatcher) { _messageDispatcher = messageDispatcher; }

    /// <inheritdoc />
    public Task RegisterRemovableMessageAsync(IUser user, EmbedBuilder embed,
        Func<EmbedBuilder, Task<IUserMessage>> callback)
        => RegisterRemovableMessageAsync(new[] { user }, embed, callback);

    /// <inheritdoc />
    public async Task RegisterRemovableMessageAsync(IUser[] user, EmbedBuilder embed,
        Func<EmbedBuilder, Task<IUserMessage>> callback)
    {
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        if (embed.Footer?.Text is null)
            embed.WithFooter(FooterReactMessage);
        else if (!embed.Footer.Text.Contains(FooterReactMessage)) embed.Footer.Text += $" | {FooterReactMessage}";

        var msg = await callback(embed);
        await _messageDispatcher.Publish(new RemovableMessageSentNotification(msg, user));
    }

    /// <inheritdoc />
    public void UnregisterRemovableMessage(IMessage message)
        => _messageDispatcher.Publish(new RemovableMessageRemovedNotification(message));
}