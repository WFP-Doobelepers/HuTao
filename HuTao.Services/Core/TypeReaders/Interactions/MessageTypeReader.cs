using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Services.Utilities;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;
using MessageExtensions = HuTao.Services.Utilities.MessageExtensions;

namespace HuTao.Services.Core.TypeReaders.Interactions;

public class MessageTypeReader<T> : TypeReader<T?> where T : class, IMessage
{
    public override async Task<TypeConverterResult> ReadAsync(IInteractionContext context, string option,
        IServiceProvider services)
    {
        if (string.IsNullOrEmpty(option))
            return TypeConverterResult.FromSuccess(null);

        if (ulong.TryParse(option, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            && await context.Channel.GetMessageAsync(id).ConfigureAwait(false) is T msg)
            return TypeConverterResult.FromSuccess(msg);

        var jump = MessageExtensions.GetJumpMessage(option);
        if (jump is null)
            return TypeConverterResult.FromError(InteractionCommandError.ParseFailed, "Not a valid jump url.");

        var message = await jump.GetMessageAsync(new InteractionContext(context));
        return message is not T result
            ? TypeConverterResult.FromError(InteractionCommandError.Unsuccessful, "Could not find message.")
            : TypeConverterResult.FromSuccess(result);
    }
}