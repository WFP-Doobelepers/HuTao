using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Zhongli.Services.Core.TypeReaders;

public class EnumerableTypeReader<TResult> : TypeReader
{
    private readonly string _separator;
    private readonly StringSplitOptions _splitOptions;
    private readonly TypeReader _typeReader;

    public EnumerableTypeReader(TypeReader typeReader, string separator = ",",
        StringSplitOptions splitOptions = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    {
        _typeReader   = typeReader;
        _separator    = separator;
        _splitOptions = splitOptions;
    }

    public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
        IServiceProvider services)
    {
        var results = await input
            .Split(_separator, _splitOptions).ToAsyncEnumerable()
            .SelectAwait(async i => await _typeReader.ReadAsync(context, i, services))
            .SelectMany(r => r.Values?.ToAsyncEnumerable() ?? AsyncEnumerable.Empty<TypeReaderValue>())
            .Select(v => v.Value).OfType<TResult>()
            .ToListAsync();

        return TypeReaderResult.FromSuccess(results);
    }
}