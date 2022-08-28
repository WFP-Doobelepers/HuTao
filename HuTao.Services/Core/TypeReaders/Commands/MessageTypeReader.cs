using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace HuTao.Services.Core.TypeReaders.Commands;

/// <summary>
///     A <see cref="TypeReader" /> for parsing objects implementing <see cref="IMessage" />.
/// </summary>
/// <typeparam name="T">The type to be checked; must implement <see cref="IMessage" />.</typeparam>
public class MessageTypeReader<T> : TypeReader where T : class, IMessage
{
    public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
        IServiceProvider services)
    {
        if (!ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return TypeReaderResult.FromError(CommandError.ParseFailed, "Could not parse Message ID.");

        if (await context.Channel.GetMessageAsync(id).ConfigureAwait(false) is T msg)
            return TypeReaderResult.FromSuccess(msg);

        return TypeReaderResult.FromError(CommandError.ObjectNotFound, "Could not find message.");
    }
}