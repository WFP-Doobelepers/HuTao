using System;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Services.Interactive.TryParse;

namespace HuTao.Services.Core.TypeReaders.Commands;

public class TryParseTypeReader<T>(TryParseDelegate<T> tryParse) : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(
        ICommandContext context, string input, IServiceProvider services) =>
        tryParse(input, out var result)
            ? Task.FromResult(TypeReaderResult.FromSuccess(result))
            : Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Invalid input"));
}

public class EnumTryParseTypeReader<T>(bool ignoreCase = true) : TypeReader
    where T : struct, Enum
{
    public override Task<TypeReaderResult> ReadAsync(
        ICommandContext context, string input, IServiceProvider services) =>
        Enum.TryParse<T>(input, ignoreCase, out var result) && Enum.IsDefined(result)
            ? Task.FromResult(TypeReaderResult.FromSuccess(result))
            : Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Failed to parse {input}."));
}