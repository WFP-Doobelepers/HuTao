using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core.TypeReaders;

public class EnumFlagsTypeReader<T> : TypeReader where T : struct, Enum
{
    private readonly bool _ignoreCase;
    private readonly string _separator;
    private readonly StringSplitOptions _splitOptions;

    public EnumFlagsTypeReader() : this(true, ", ") { }

    public EnumFlagsTypeReader(bool ignoreCase = true, string separator = ",",
        StringSplitOptions splitOptions = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    {
        _splitOptions = splitOptions;
        _ignoreCase   = ignoreCase;
        _separator    = separator;
    }

    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
        IServiceProvider services)
    {
        var enums = input.Split(_separator, _splitOptions)
            .Select(content => (success: Enum.TryParse<T>(content, _ignoreCase, out var result), result))
            .Where(e => e.success)
            .ToList();

        var generic = new GenericBitwise<T>();

        return enums.Any()
            ? Task.FromResult(TypeReaderResult.FromSuccess(generic.Or(enums.Select(e => e.result))))
            : Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Failed to parse input."));
    }
}