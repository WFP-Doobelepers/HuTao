using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Zhongli.Services.Core.TypeReaders
{
    public class EnumerableTypeReader<TReader, TResult> : TypeReader
        where TReader : TypeReader, new()
        where TResult : class
    {
        private readonly string _separator;
        private readonly StringSplitOptions _splitOptions;
        private readonly TReader _typeReader;

        public EnumerableTypeReader(TReader? typeReader = null, string separator = ",",
            StringSplitOptions splitOptions = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        {
            _typeReader   = typeReader ?? new TReader();
            _separator    = separator;
            _splitOptions = splitOptions;
        }

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var results = await input
                .Split(_separator, _splitOptions).ToAsyncEnumerable()
                .SelectAwait(async i => await _typeReader.ReadAsync(context, i, services))
                .SelectMany(r => r.Values.ToAsyncEnumerable())
                .Select(v => v.Value).OfType<TResult>()
                .ToListAsync();

            return TypeReaderResult.FromSuccess(results);
        }
    }
}