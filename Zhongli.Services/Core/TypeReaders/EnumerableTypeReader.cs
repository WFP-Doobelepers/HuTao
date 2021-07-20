using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Zhongli.Services.Core.TypeReaders
{
    public class EnumerableTypeReader<TReader, TResult> : TypeReader
        where TReader : TypeReader
        where TResult : class
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var commands = services.GetRequiredService<CommandService>();

            var typeReader = commands.TypeReaders
                .FirstOrDefault(t => t.Key == typeof(TReader));

            if (typeReader is null)
            {
                return TypeReaderResult.FromError(CommandError.ObjectNotFound,
                    $"The type reader {typeof(TReader)} could not be found.");
            }

            var results = await input.Split(" ")
                .ToAsyncEnumerable()
                .SelectMany(i => typeReader
                    .ToAsyncEnumerable()
                    .SelectAwait(async t => await t.ReadAsync(context, i, services))
                    .SelectMany(r => r.Values.ToAsyncEnumerable())
                )
                .Select(r => r.Value as TResult)
                .ToListAsync();

            return TypeReaderResult.FromSuccess(results);
        }
    }
}