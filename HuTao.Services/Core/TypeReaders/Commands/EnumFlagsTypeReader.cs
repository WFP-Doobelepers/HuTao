using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Services.Utilities;

namespace HuTao.Services.Core.TypeReaders.Commands;

public class EnumFlagsTypeReader<T>(
    bool ignoreCase = true,
    string separator = ",",
    StringSplitOptions splitOptions = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    : TypeReader
    where T : struct, Enum
{
    public EnumFlagsTypeReader() : this(true, ", ") { }

    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
        IServiceProvider services)
    {
        var enums = input.Split(separator, splitOptions)
            .Select(content => (Success: Enum.TryParse<T>(content, ignoreCase, out var result), Result: result))
            .ToList();

        var generic = new GenericBitwise<T>();

        return enums.All(e => e.Success)
            ? Task.FromResult(TypeReaderResult.FromSuccess(generic.Or(enums.Select(e => e.Result))))
            : Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Failed to parse input."));
    }
}