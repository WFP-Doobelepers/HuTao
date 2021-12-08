using System;
using System.Threading.Tasks;
using Discord.Commands;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core.TypeReaders;

public class JumpUrlTypeReader : TypeReader
{
    public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
        IServiceProvider services)
    {
        var message = await context.GetMessageFromUrlAsync(input);

        return message is null
            ? TypeReaderResult.FromError(CommandError.ParseFailed, "Could not find message.")
            : TypeReaderResult.FromSuccess(message);
    }
}