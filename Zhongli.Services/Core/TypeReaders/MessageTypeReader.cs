using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core.TypeReaders
{
    /// <summary>
    ///     A <see cref="TypeReader" /> for parsing objects implementing <see cref="IMessage" />.
    /// </summary>
    /// <typeparam name="T">The type to be checked; must implement <see cref="IMessage" />.</typeparam>
    public class MessageTypeReader<T> : TypeReader where T : class, IMessage
    {
        /// <inheritdoc />
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            {
                if (await context.GetMessageAsync(id).ConfigureAwait(false) is T msg)
                    return TypeReaderResult.FromSuccess(msg);
            }

            return TypeReaderResult.FromError(CommandError.ObjectNotFound, "Message not found.");
        }
    }
}