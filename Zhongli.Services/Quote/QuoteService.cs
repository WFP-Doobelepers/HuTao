using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Zhongli.Services.AutoRemoveMessage;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Quote;

public interface IQuoteService
{
    /// <summary>
    ///     Build an embed quote for the given message. Returns null if the message could not be quoted.
    /// </summary>
    /// <param name="message">The message to quote</param>
    /// <param name="executingUser">The user that is doing the quoting</param>
    EmbedBuilder? BuildQuoteEmbed(IMessage message, IUser executingUser);

    Task BuildRemovableEmbed(IMessage message, IUser executingUser,
        Func<EmbedBuilder, Task<IUserMessage>>? callback);
}

public class QuoteService : IQuoteService
{
    private readonly IAutoRemoveMessageService _autoRemoveMessageService;

    public QuoteService(IAutoRemoveMessageService autoRemoveMessageService)
    {
        _autoRemoveMessageService = autoRemoveMessageService;
    }

    /// <inheritdoc />
    public EmbedBuilder? BuildQuoteEmbed(IMessage message, IUser executingUser)
    {
        if (IsQuote(message)) return null;

        var embed = message.GetRichEmbed() ?? new EmbedBuilder();

        if (!embed.TryAddImageAttachment(message))
        {
            if (!embed.TryAddImageEmbed(message))
            {
                if (!embed.TryAddThumbnailEmbed(message))
                    embed.TryAddOtherAttachment(message);
            }
        }

        embed.WithColor(new Color(95, 186, 125))
            .AddContent(message)
            .AddOtherEmbed(message)
            .AddActivity(message)
            .AddMeta(message, AuthorOptions.IncludeId)
            .AddJumpLink(message, executingUser);

        return embed;
    }

    public async Task BuildRemovableEmbed(IMessage message, IUser executingUser,
        Func<EmbedBuilder, Task<IUserMessage>>? callback)
    {
        var embed = BuildQuoteEmbed(message, executingUser);

        if (callback is null || embed is null) return;

        await _autoRemoveMessageService.RegisterRemovableMessageAsync(executingUser, embed,
            async e => await callback.Invoke(e));
    }

    private static bool IsQuote(IMessage message) => message
        .Embeds?
        .SelectMany(d => d.Fields)
        .Any(d => d.Name == "Quoted by") == true;
}