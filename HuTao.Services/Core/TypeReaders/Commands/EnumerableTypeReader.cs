using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace HuTao.Services.Core.TypeReaders.Commands;

public class EnumerableTypeReader<TResult>(
    TypeReader typeReader,
    string[]? separators = null,
    StringSplitOptions splitOptions = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    : TypeReader
{
    private readonly string[] _separators = separators ?? [",", " ", "\r\n", "\r", "\n"];

    public override async Task<TypeReaderResult> ReadAsync(
        ICommandContext context, string input,
        IServiceProvider services)
    {
        var results = await input
            .Split(_separators, splitOptions).ToAsyncEnumerable()
            .SelectAwait(async i => await typeReader.ReadAsync(context, i, services))
            .SelectMany(r => r.Values?.ToAsyncEnumerable() ?? AsyncEnumerable.Empty<TypeReaderValue>())
            .Select(v => v.Value).OfType<TResult>()
            .ToListAsync();

        return TypeReaderResult.FromSuccess(results);
    }
}