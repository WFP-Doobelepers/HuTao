using System;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Services.Utilities;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;
using MessageExtensions = HuTao.Services.Utilities.MessageExtensions;

namespace HuTao.Services.Core.TypeReaders.Commands;

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