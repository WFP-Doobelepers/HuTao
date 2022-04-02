using System;
using System.Threading.Tasks;
using Discord.Commands;
using Zhongli.Services.Utilities;
using CommandContext = Zhongli.Data.Models.Discord.CommandContext;
using MessageExtensions = Zhongli.Services.Utilities.MessageExtensions;

namespace Zhongli.Services.Core.TypeReaders;

public class JumpUrlTypeReader : TypeReader
{
    public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
        IServiceProvider services)
    {
        var jump = MessageExtensions.GetJumpMessage(input);
        if (jump is null) return TypeReaderResult.FromError(CommandError.ParseFailed, "Not a valid jump url.");

        var message = await jump.GetMessageAsync(new CommandContext(context));
        return message is null
            ? TypeReaderResult.FromError(CommandError.Unsuccessful, "Could not find message.")
            : TypeReaderResult.FromSuccess(message);
    }
}